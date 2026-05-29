using System;
using System.Collections.Generic;
using CompanySimulator.Features.Banking.Runtime.Components;
using CompanySimulator.Features.Employees.Runtime.Components;
using CompanySimulator.Features.Employees.Runtime.Models;
using CompanySimulator.Features.Finance.Runtime.Definitions;
using CompanySimulator.Features.Finance.Runtime.Models;
using CompanySimulator.Features.Finance.Runtime.Services;
using CompanySimulator.Features.Investments.Runtime.Definitions;
using CompanySimulator.Features.Projects.Runtime.Definitions;
using CompanySimulator.Features.Save.Runtime.Models;
using CompanySimulator.Features.Save.Runtime.Services;
using CompanySimulator.Features.Time.Runtime.Components;
using CompanySimulator.Features.Sectors.Runtime.Services;
using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;

namespace CompanySimulator.Features.Finance.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class EconomyManager : MonoBehaviour
    {
        [SerializeField] private EconomySetupDefinition setup;
        [SerializeField] private EmployeeManager employeeManager;
        [SerializeField] private CompanyBankManager companyBankManager;
        [SerializeField] private TimeManager timeManager;
        [SerializeField] private bool initializeOnAwake = true;
        [SerializeField] private bool runStartupProjectsOnStart = true;
        [SerializeField] private int currentDay = 1;
        [SerializeField] private long currentBalance;
        [SerializeField] private int executedProjectCount;
        [SerializeField] private string lastExecutionSummary;

        private CompanyLedger ledger;
        private ProjectEconomyCalculator calculator;
        private readonly List<ProjectExecutionHistoryEntry> executionHistory = new List<ProjectExecutionHistoryEntry>(32);
        private readonly List<ActiveProjectRuntimeEntry> activeProjects = new List<ActiveProjectRuntimeEntry>(32);
        private bool isInitialized;

        public event Action<Money> BalanceChanged;
        public event Action<int> DayAdvanced;
        public event Action<LedgerEntry> LedgerEntryRecorded;
        public event Action<ProjectExecutionDefinition, ProjectEconomyResult> ProjectExecuted;
        public event Action<ProjectExecutionDefinition, ProjectEconomyResult> ProjectExecutionRejected;
        public event Action<ActiveProjectRuntimeEntry> ProjectSold;

        public bool IsInitialized => isInitialized;
        public int CurrentDay => currentDay;
        public Money Balance => ledger != null ? ledger.Balance : Money.Zero;
        public IReadOnlyList<LedgerEntry> Entries => ledger != null ? ledger.Entries : Array.Empty<LedgerEntry>();
        public IReadOnlyList<ProjectExecutionHistoryEntry> ExecutionHistory => executionHistory;
        public IReadOnlyList<ActiveProjectRuntimeEntry> ActiveProjects => activeProjects;
        public string LastExecutionSummary => lastExecutionSummary;
        public int ExecutedProjectCount => executedProjectCount;

        private void Awake()
        {
            employeeManager ??= FindObjectOfType<EmployeeManager>();
            timeManager ??= FindObjectOfType<TimeManager>();
            if (timeManager == null)
            {
                timeManager = new GameObject("TimeManager", typeof(TimeManager)).GetComponent<TimeManager>();
            }

            if (initializeOnAwake)
            {
                Initialize();
            }
        }

        private void Start()
        {
            if (runStartupProjectsOnStart)
            {
                RunStartupProjects();
            }
        }

        [ContextMenu("Initialize Economy")]
        public void Initialize()
        {
            if (setup == null)
            {
                Debug.LogError("EconomyManager için kurulum verisi atanmadı.", this);
                isInitialized = false;
                return;
            }

            if (setup.BalanceDefinition == null)
            {
                Debug.LogError("EconomyManager için ekonomi balans verisi atanmadı.", this);
                isInitialized = false;
                return;
            }

            // Yeni başlangıçta eski oturum verileri tamamen temizlenir.
            ledger ??= new CompanyLedger();
            ledger.Clear();
            calculator = new ProjectEconomyCalculator(setup.BalanceDefinition);
            executionHistory.Clear();
            activeProjects.Clear();
            currentDay = 1;
            executedProjectCount = 0;
            lastExecutionSummary = string.Empty;
            isInitialized = true;

            if (setup.StartingCapital > Money.Zero)
            {
                RecordIncome(setup.StartingCapital, LedgerEntryType.InitialCapital, "Başlangıç sermayesi");
            }

            UpdateSnapshot();
            BalanceChanged?.Invoke(Balance);
        }

        [ContextMenu("Run Startup Projects")]
        public int RunStartupProjects()
        {
            if (!EnsureInitialized())
            {
                return 0;
            }

            var executedCount = 0;
            var startupProjects = setup.StartupProjects;
            for (var i = 0; i < startupProjects.Count; i++)
            {
                if (TryExecuteProject(startupProjects[i], out _))
                {
                    executedCount++;
                }
            }

            return executedCount;
        }

        [ContextMenu("Advance Day")]
        public void AdvanceDay()
        {
            if (!EnsureInitialized())
            {
                return;
            }

            currentDay++;
            ProcessDailyPayroll();
            ProcessRecurringPayouts();
            UpdateSnapshot();
            BalanceChanged?.Invoke(Balance);
            DayAdvanced?.Invoke(currentDay);
        }

        public ProjectEconomyResult PreviewProject(ProjectExecutionDefinition executionDefinition)
        {
            if (!EnsureInitialized())
            {
                throw new InvalidOperationException("EconomyManager henüz başlatılmadı.");
            }

            if (executionDefinition == null)
            {
                throw new ArgumentNullException(nameof(executionDefinition));
            }

            return PreviewProject(executionDefinition.CreateRequest());
        }

        public ProjectEconomyResult PreviewProject(ProjectEconomyRequest request)
        {
            if (!EnsureInitialized())
            {
                throw new InvalidOperationException("EconomyManager henüz başlatılmadı.");
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return calculator.Calculate(request);
        }

        public bool CanExecuteProject(ProjectExecutionDefinition executionDefinition, out ProjectEconomyResult result)
        {
            result = PreviewProject(executionDefinition);
            return Balance >= result.TotalCosts;
        }

        public bool CanExecuteProject(ProjectEconomyRequest request, out ProjectEconomyResult result)
        {
            result = PreviewProject(request);
            return Balance >= result.TotalCosts;
        }

        public bool CanSpend(Money amount)
        {
            if (!EnsureInitialized() || amount < Money.Zero)
            {
                return false;
            }

            return Balance >= amount;
        }

        public bool TrySpend(Money amount, LedgerEntryType expenseType, string description)
        {
            if (!EnsureInitialized())
            {
                return false;
            }

            if (amount < Money.Zero)
            {
                return false;
            }

            if (amount == Money.Zero)
            {
                return true;
            }

            if (Balance < amount)
            {
                return false;
            }

            ApplyExpense(amount, expenseType, description);
            UpdateSnapshot();
            BalanceChanged?.Invoke(Balance);
            return true;
        }

        public bool TrySpendWithAutoLoan(Money amount, LedgerEntryType expenseType, string description)
        {
            if (!EnsureInitialized())
            {
                return false;
            }

            if (amount < Money.Zero)
            {
                return false;
            }

            if (amount == Money.Zero)
            {
                return true;
            }

            if (Balance < amount)
            {
                var deficit = amount - Balance;
                companyBankManager ??= FindObjectOfType<CompanyBankManager>();
                if (companyBankManager == null || !companyBankManager.TryAutoLoan(deficit))
                {
                    return false;
                }

                if (Balance < amount)
                {
                    return false;
                }
            }

            ApplyExpense(amount, expenseType, description);
            UpdateSnapshot();
            BalanceChanged?.Invoke(Balance);
            return true;
        }

        public bool TryExecuteProject(ProjectExecutionDefinition executionDefinition, out ProjectEconomyResult result)
        {
            result = default;

            if (!EnsureInitialized())
            {
                return false;
            }

            if (executionDefinition == null)
            {
                Debug.LogWarning("Çalıştırılacak iş tanımı bulunamadı.", this);
                return false;
            }

            return TryExecuteProject(executionDefinition, executionDefinition.CreateRequest(), executionDefinition.DisplayName, null, out result);
        }

        public bool TryExecuteProject(ProjectExecutionDefinition sourceDefinition, ProjectEconomyRequest request, string displayName, out ProjectEconomyResult result)
        {
            return TryExecuteProject(sourceDefinition, request, displayName, null, null, null, out result);
        }

        public bool TryExecuteProject(ProjectExecutionDefinition sourceDefinition, ProjectEconomyRequest request, string displayName, IReadOnlyList<string> assignedEmployeeNames, out ProjectEconomyResult result)
        {
            return TryExecuteProject(sourceDefinition, request, displayName, null, null, assignedEmployeeNames, out result);
        }

        public bool TryExecuteProject(ProjectExecutionDefinition sourceDefinition, ProjectEconomyRequest request, string displayName, IReadOnlyList<EmployeeRuntimeData> assignedEmployees, IReadOnlyList<string> assignedEmployeeSlotIds, IReadOnlyList<string> assignedEmployeeNames, out ProjectEconomyResult result)
        {
            result = default;

            if (!EnsureInitialized())
            {
                return false;
            }

            if (request == null)
            {
                Debug.LogWarning("Çalıştırılacak iş isteği bulunamadı.", this);
                return false;
            }

            var safeDisplayName = string.IsNullOrWhiteSpace(displayName)
                ? request.ProjectType != null ? request.ProjectType.DisplayName : "İsimsiz İş"
                : displayName;

            result = calculator.Calculate(request);
            if (Balance < result.TotalCosts)
            {
                lastExecutionSummary = $"{safeDisplayName}: yetersiz bakiye.";
                UpdateSnapshot();
                if (sourceDefinition != null)
                {
                    ProjectExecutionRejected?.Invoke(sourceDefinition, result);
                }

                return false;
            }

            ApplyExpense(result.FixedCost, LedgerEntryType.MiscExpense, $"{safeDisplayName} sabit gider");
            ApplyExpense(result.UpfrontInvestmentCost, LedgerEntryType.InvestmentExpense, $"{safeDisplayName} peşin yatırım gideri");
            var activeProject = new ActiveProjectRuntimeEntry(
                sourceDefinition,
                safeDisplayName,
                request.ProjectType,
                result,
                currentDay,
                assignedEmployees != null ? ToArray(assignedEmployees) : null,
                assignedEmployeeSlotIds != null ? ToArray(assignedEmployeeSlotIds) : null,
                assignedEmployeeNames != null ? ToArray(assignedEmployeeNames) : null,
                request.InvestmentAllocations != null ? ToArray(request.InvestmentAllocations) : null,
                request.MarketDemandMultiplier,
                request.CompetitorPressure);
            activeProjects.Add(activeProject);
            executedProjectCount++;
            lastExecutionSummary = $"{safeDisplayName}: iş başladı, {activeProject.PayoutIntervalDays} günde bir gelir döngüsü oluşturacak.";
            UpdateSnapshot();
            BalanceChanged?.Invoke(Balance);
            if (sourceDefinition != null)
            {
                ProjectExecuted?.Invoke(sourceDefinition, result);
            }

            return true;
        }

        public bool TryUpdateActiveProject(ActiveProjectRuntimeEntry activeProject, ProjectEconomyRequest request, IReadOnlyList<EmployeeRuntimeData> assignedEmployees, IReadOnlyList<string> assignedEmployeeSlotIds, IReadOnlyList<string> assignedEmployeeNames, out ProjectEconomyResult result, out string validationMessage)
        {
            result = default;
            validationMessage = string.Empty;

            if (!EnsureInitialized())
            {
                validationMessage = "Ekonomi sistemi hazır değil.";
                return false;
            }

            if (activeProject == null || request == null || !activeProjects.Contains(activeProject))
            {
                validationMessage = "Düzenlenecek aktif iş bulunamadı.";
                return false;
            }

            if (!AreInvestmentAllocationsNonDecreasing(activeProject, request.InvestmentAllocations))
            {
                validationMessage = "Aktif işte yatırım bütçeleri azaltılamaz; yalnızca artırılabilir.";
                return false;
            }

            result = calculator.Calculate(request);
            var additionalUpfrontCost = result.UpfrontInvestmentCost - activeProject.CurrentResult.UpfrontInvestmentCost;
            if (additionalUpfrontCost > Money.Zero)
            {
                if (Balance < additionalUpfrontCost)
                {
                    validationMessage = "Yatırım artışı için yeterli bakiye yok.";
                    return false;
                }

                ApplyExpense(additionalUpfrontCost, LedgerEntryType.InvestmentExpense, $"{activeProject.DisplayName} güncelleme yatırım gideri");
            }

            activeProject.UpdateConfiguration(
                result,
                assignedEmployees != null ? ToArray(assignedEmployees) : null,
                assignedEmployeeSlotIds != null ? ToArray(assignedEmployeeSlotIds) : null,
                assignedEmployeeNames != null ? ToArray(assignedEmployeeNames) : null,
                request.InvestmentAllocations != null ? ToArray(request.InvestmentAllocations) : null,
                currentDay);

            lastExecutionSummary = $"{activeProject.DisplayName}: aktif iş güncellendi, gelir döngüsü sıfırlandı.";
            UpdateSnapshot();
            BalanceChanged?.Invoke(Balance);
            return true;
        }

        public bool TrySellProject(ActiveProjectRuntimeEntry activeProject, out Money saleValue)
        {
            saleValue = Money.Zero;

            if (!EnsureInitialized() || activeProject == null || !activeProjects.Contains(activeProject))
            {
                return false;
            }

            var sector = activeProject.Sector;
            var competitionMultiplier = SectorCompetitionService.GetCachedRevenueMultiplier(sector);
            var agentMultiplier = activeProject.IsAgentAffected ? activeProject.AgentRevenueReductionMultiplier : 1f;
            var adjustedRevenue = Money.From(activeProject.CycleRevenue.Amount * competitionMultiplier * agentMultiplier);
            var saleMultiplier = sector != null ? sector.SaleRevenueMultiplier : 1f;
            saleValue = Money.From(adjustedRevenue.Amount * saleMultiplier);

            activeProjects.Remove(activeProject);

            if (saleValue > Money.Zero)
            {
                RecordIncomeInternal(saleValue, LedgerEntryType.ProjectSaleIncome, $"{activeProject.DisplayName} iş satışı");
            }

            if (sector != null && sector.CompetitionLingerDays > 0)
            {
                SectorCompetitionService.RegisterLingeringProject(sector, sector.CompetitionLingerDays);
            }

            if (employeeManager != null)
            {
                var assignedEmployees = activeProject.AssignedEmployees;
                for (var i = 0; i < assignedEmployees.Count; i++)
                {
                    var employee = assignedEmployees[i];
                    if (employee != null)
                    {
                        employeeManager.ForceRemoveEmployee(employee);
                    }
                }
            }

            lastExecutionSummary = $"{activeProject.DisplayName}: iş satıldı. Satış tutarı: {saleValue.Amount:N0}";
            UpdateSnapshot();
            BalanceChanged?.Invoke(Balance);
            ProjectSold?.Invoke(activeProject);
            return true;
        }

        public void RecordIncome(Money amount, LedgerEntryType type, string description)
        {
            if (!EnsureInitialized())
            {
                return;
            }

            RecordIncomeInternal(amount, type, description);
            UpdateSnapshot();
            BalanceChanged?.Invoke(Balance);
        }

        public bool TryRecordExpense(Money amount, LedgerEntryType type, string description)
        {
            if (!EnsureInitialized())
            {
                return false;
            }

            if (amount <= Money.Zero || Balance < amount)
            {
                return false;
            }

            ApplyExpense(amount, type, description);
            UpdateSnapshot();
            BalanceChanged?.Invoke(Balance);
            return true;
        }

        public EconomySaveData CaptureSaveData()
        {
            EnsureInitialized();

            var saveData = new EconomySaveData
            {
                currentDay = currentDay,
                balance = Balance.Amount,
                executedProjectCount = executedProjectCount,
                lastExecutionSummary = lastExecutionSummary
            };

            var entries = Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                saveData.ledgerEntries.Add(new LedgerEntrySaveData
                {
                    day = entry.Day,
                    type = (int)entry.Type,
                    amount = entry.Amount.Amount,
                    description = entry.Description
                });
            }

            for (var i = 0; i < activeProjects.Count; i++)
            {
                if (activeProjects[i] != null)
                {
                    saveData.activeProjects.Add(CaptureActiveProject(activeProjects[i]));
                }
            }

            for (var i = 0; i < executionHistory.Count; i++)
            {
                var history = executionHistory[i];
                saveData.executionHistory.Add(new ProjectHistorySaveData
                {
                    sourceDefinitionId = history.SourceDefinition != null ? history.SourceDefinition.Id : string.Empty,
                    projectTypeId = history.ProjectType != null ? history.ProjectType.Id : string.Empty,
                    displayName = history.DisplayName,
                    result = CaptureProjectResult(history.Result)
                });
            }

            return saveData;
        }

        public bool RestoreFromSaveData(
            EconomySaveData saveData,
            GameSaveDefinitionResolver resolver,
            IReadOnlyDictionary<string, EmployeeRuntimeData> employeeLookup,
            out string validationMessage)
        {
            validationMessage = string.Empty;
            if (saveData == null)
            {
                validationMessage = "Ekonomi kayıt verisi bulunamadı.";
                return false;
            }

            if (setup == null || setup.BalanceDefinition == null)
            {
                validationMessage = "Ekonomi kurulum verisi eksik.";
                return false;
            }

            if (!ValidateEconomySaveData(saveData, resolver, employeeLookup, out validationMessage))
            {
                return false;
            }

            ledger ??= new CompanyLedger();
            var restoredEntries = new List<LedgerEntry>(saveData.ledgerEntries.Count);
            for (var i = 0; i < saveData.ledgerEntries.Count; i++)
            {
                var entry = saveData.ledgerEntries[i];
                restoredEntries.Add(new LedgerEntry(
                    Mathf.Max(1, entry.day),
                    (LedgerEntryType)entry.type,
                    Money.From(entry.amount),
                    entry.description));
            }

            ledger.Restore(restoredEntries, Money.From(saveData.balance));
            calculator = new ProjectEconomyCalculator(setup.BalanceDefinition);
            activeProjects.Clear();
            executionHistory.Clear();
            currentDay = Mathf.Max(1, saveData.currentDay);
            executedProjectCount = Mathf.Max(0, saveData.executedProjectCount);
            lastExecutionSummary = saveData.lastExecutionSummary ?? string.Empty;

            for (var i = 0; i < saveData.activeProjects.Count; i++)
            {
                activeProjects.Add(RestoreActiveProject(saveData.activeProjects[i], resolver, employeeLookup));
            }

            for (var i = 0; i < saveData.executionHistory.Count; i++)
            {
                var history = saveData.executionHistory[i];
                resolver.TryResolve<ProjectExecutionDefinition>(history.sourceDefinitionId, out var sourceDefinition);
                var projectType = sourceDefinition != null ? sourceDefinition.ProjectType : null;
                if (projectType == null && !string.IsNullOrWhiteSpace(history.projectTypeId))
                {
                    resolver.TryResolve<ProjectTypeDefinition>(history.projectTypeId, out projectType);
                }

                executionHistory.Add(new ProjectExecutionHistoryEntry(
                    sourceDefinition,
                    history.displayName,
                    projectType,
                    RestoreProjectResult(history.result)));
            }

            isInitialized = true;
            UpdateSnapshot();
            BalanceChanged?.Invoke(Balance);
            return true;
        }

        private bool EnsureInitialized()
        {
            if (isInitialized)
            {
                return true;
            }

            Initialize();
            return isInitialized;
        }

        private static ActiveProjectSaveData CaptureActiveProject(ActiveProjectRuntimeEntry activeProject)
        {
            var saveData = new ActiveProjectSaveData
            {
                sourceDefinitionId = activeProject.SourceDefinition != null ? activeProject.SourceDefinition.Id : string.Empty,
                projectTypeId = activeProject.ProjectType != null ? activeProject.ProjectType.Id : string.Empty,
                displayName = activeProject.DisplayName,
                startedDay = activeProject.StartedDay,
                nextPayoutDay = activeProject.NextPayoutDay,
                payoutCount = activeProject.PayoutCount,
                result = CaptureProjectResult(activeProject.CurrentResult),
                marketDemandMultiplier = activeProject.MarketDemandMultiplier,
                competitorPressure = activeProject.CompetitorPressure,
                isAgentAffected = activeProject.IsAgentAffected,
                agentRevenueReductionMultiplier = activeProject.AgentRevenueReductionMultiplier
            };

            var assignedEmployees = activeProject.AssignedEmployees;
            for (var i = 0; i < assignedEmployees.Count; i++)
            {
                saveData.assignedEmployeeIds.Add(assignedEmployees[i] != null ? assignedEmployees[i].Id : string.Empty);
            }

            var assignedSlotIds = activeProject.AssignedEmployeeSlotIds;
            for (var i = 0; i < assignedSlotIds.Count; i++)
            {
                saveData.assignedEmployeeSlotIds.Add(assignedSlotIds[i] ?? string.Empty);
            }

            var assignedNames = activeProject.AssignedEmployeeNames;
            for (var i = 0; i < assignedNames.Count; i++)
            {
                saveData.assignedEmployeeNames.Add(assignedNames[i] ?? string.Empty);
            }

            var allocations = activeProject.CurrentInvestmentAllocations;
            for (var i = 0; i < allocations.Count; i++)
            {
                var allocation = allocations[i];
                saveData.investmentAllocations.Add(new InvestmentAllocationSaveData
                {
                    investmentTypeId = allocation.InvestmentType != null ? allocation.InvestmentType.Id : string.Empty,
                    allocatedBudget = allocation.AllocatedBudgetAmount
                });
            }

            return saveData;
        }

        private static ProjectResultSaveData CaptureProjectResult(ProjectEconomyResult result)
        {
            return new ProjectResultSaveData
            {
                durationDays = result.DurationDays,
                revenue = result.Revenue.Amount,
                payrollCost = result.PayrollCost.Amount,
                upfrontInvestmentCost = result.UpfrontInvestmentCost.Amount,
                recurringInvestmentCost = result.RecurringInvestmentCost.Amount,
                fixedCost = result.FixedCost.Amount,
                successScore = result.SuccessScore,
                employeeContribution = result.EmployeeContribution,
                investmentContribution = result.InvestmentContribution,
                competitionMultiplier = result.CompetitionMultiplier
            };
        }

        private static bool ValidateEconomySaveData(
            EconomySaveData saveData,
            GameSaveDefinitionResolver resolver,
            IReadOnlyDictionary<string, EmployeeRuntimeData> employeeLookup,
            out string validationMessage)
        {
            validationMessage = string.Empty;
            if (resolver == null)
            {
                validationMessage = "Tanım çözücü bulunamadı.";
                return false;
            }

            for (var i = 0; i < saveData.activeProjects.Count; i++)
            {
                var activeProject = saveData.activeProjects[i];
                if (!string.IsNullOrWhiteSpace(activeProject.sourceDefinitionId) &&
                    !resolver.TryResolve<ProjectExecutionDefinition>(activeProject.sourceDefinitionId, out _))
                {
                    validationMessage = $"Aktif iş tanımı bulunamadı: {activeProject.sourceDefinitionId}";
                    return false;
                }

                for (var employeeIndex = 0; employeeIndex < activeProject.assignedEmployeeIds.Count; employeeIndex++)
                {
                    var employeeId = activeProject.assignedEmployeeIds[employeeIndex];
                    if (!string.IsNullOrWhiteSpace(employeeId) &&
                        (employeeLookup == null || !employeeLookup.ContainsKey(employeeId)))
                    {
                        validationMessage = $"Aktif iş çalışanı bulunamadı: {employeeId}";
                        return false;
                    }
                }

                for (var allocationIndex = 0; allocationIndex < activeProject.investmentAllocations.Count; allocationIndex++)
                {
                    var investmentId = activeProject.investmentAllocations[allocationIndex].investmentTypeId;
                    if (!string.IsNullOrWhiteSpace(investmentId) &&
                        !resolver.TryResolve<InvestmentTypeDefinition>(investmentId, out _))
                    {
                        validationMessage = $"Yatırım tanımı bulunamadı: {investmentId}";
                        return false;
                    }
                }
            }

            return true;
        }

        private static ActiveProjectRuntimeEntry RestoreActiveProject(
            ActiveProjectSaveData saveData,
            GameSaveDefinitionResolver resolver,
            IReadOnlyDictionary<string, EmployeeRuntimeData> employeeLookup)
        {
            resolver.TryResolve<ProjectExecutionDefinition>(saveData.sourceDefinitionId, out var sourceDefinition);
            var projectType = sourceDefinition != null ? sourceDefinition.ProjectType : null;
            if (projectType == null && !string.IsNullOrWhiteSpace(saveData.projectTypeId))
            {
                resolver.TryResolve<ProjectTypeDefinition>(saveData.projectTypeId, out projectType);
            }

            var assignedEmployees = new List<EmployeeRuntimeData>(saveData.assignedEmployeeIds.Count);
            for (var i = 0; i < saveData.assignedEmployeeIds.Count; i++)
            {
                var employeeId = saveData.assignedEmployeeIds[i];
                assignedEmployees.Add(!string.IsNullOrWhiteSpace(employeeId) && employeeLookup != null && employeeLookup.TryGetValue(employeeId, out var employee) ? employee : null);
            }

            var investmentAllocations = new List<InvestmentAllocationInput>(saveData.investmentAllocations.Count);
            for (var i = 0; i < saveData.investmentAllocations.Count; i++)
            {
                var savedAllocation = saveData.investmentAllocations[i];
                resolver.TryResolve<InvestmentTypeDefinition>(savedAllocation.investmentTypeId, out var investmentType);
                investmentAllocations.Add(new InvestmentAllocationInput(investmentType, savedAllocation.allocatedBudget));
            }

            var restoredProject = new ActiveProjectRuntimeEntry(
                sourceDefinition,
                saveData.displayName,
                projectType,
                RestoreProjectResult(saveData.result),
                Mathf.Max(1, saveData.startedDay),
                assignedEmployees.ToArray(),
                saveData.assignedEmployeeSlotIds.ToArray(),
                saveData.assignedEmployeeNames.ToArray(),
                investmentAllocations.ToArray(),
                saveData.marketDemandMultiplier,
                saveData.competitorPressure);

            restoredProject.RestoreProgress(
                saveData.nextPayoutDay,
                saveData.payoutCount,
                saveData.isAgentAffected,
                saveData.agentRevenueReductionMultiplier);
            return restoredProject;
        }

        private static ProjectEconomyResult RestoreProjectResult(ProjectResultSaveData saveData)
        {
            if (saveData == null)
            {
                return default;
            }

            return new ProjectEconomyResult(
                Mathf.Max(0, saveData.durationDays),
                Money.From(saveData.revenue),
                Money.From(saveData.payrollCost),
                Money.From(saveData.upfrontInvestmentCost),
                Money.From(saveData.recurringInvestmentCost),
                Money.From(saveData.fixedCost),
                saveData.successScore,
                saveData.employeeContribution,
                saveData.investmentContribution,
                saveData.competitionMultiplier);
        }

        private void ApplyExpense(Money amount, LedgerEntryType type, string description)
        {
            if (amount <= Money.Zero)
            {
                return;
            }

            if (!ledger.TryRecordExpense(currentDay, amount, type, description, out var entry))
            {
                throw new InvalidOperationException("Bakiye kontrolü geçildiği halde gider uygulanamadı.");
            }

            LedgerEntryRecorded?.Invoke(entry);
        }

        private void RecordIncomeInternal(Money amount, LedgerEntryType type, string description)
        {
            if (amount <= Money.Zero)
            {
                return;
            }

            var entry = ledger.RecordIncome(currentDay, amount, type, description);
            LedgerEntryRecorded?.Invoke(entry);
        }

        private void ProcessDailyPayroll()
        {
            if (employeeManager == null)
            {
                employeeManager = FindObjectOfType<EmployeeManager>();
            }

            if (employeeManager == null)
            {
                return;
            }

            if (!employeeManager.IsInitialized)
            {
                employeeManager.Initialize();
            }

            if (timeManager == null)
            {
                timeManager = FindObjectOfType<TimeManager>();
            }

            var employees = employeeManager.Employees;
            var totalDailyPayroll = Money.Zero;
            var totalOvertimePayroll = Money.Zero;
            var overtimeHours = timeManager != null ? timeManager.GetOfficeOvertimeHours() : 0f;
            var standardWorkdayHours = timeManager != null ? timeManager.WorkdayDurationHours : 8f;
            for (var i = 0; i < employees.Count; i++)
            {
                var employee = employees[i];
                if (employee == null)
                {
                    continue;
                }

                totalDailyPayroll += employee.EffectiveDailySalary;

                if (overtimeHours <= 0f || employee.Role == null || !employee.Role.RequiresOffice)
                {
                    continue;
                }

                var hourlyRate = employee.EffectiveDailySalary.Amount / standardWorkdayHours;
                var overtimeAmount = Money.From(hourlyRate * overtimeHours * 2d);
                totalOvertimePayroll += overtimeAmount;
            }

            var totalPayrollExpense = totalDailyPayroll + totalOvertimePayroll;

            if (totalPayrollExpense <= Money.Zero)
            {
                return;
            }

            if (Balance < totalPayrollExpense)
            {
                var deficit = totalPayrollExpense - Balance;
                if (companyBankManager == null || !companyBankManager.TryAutoLoan(deficit))
                {
                    lastExecutionSummary = $"{currentDay}. günde günlük maaş gideri için bakiye yetersiz.";
                    return;
                }

                if (Balance < totalPayrollExpense)
                {
                    lastExecutionSummary = $"{currentDay}. günde günlük maaş gideri için alınan kredi gideri karşılamadı.";
                    return;
                }
            }

            var description = totalOvertimePayroll > Money.Zero
                ? $"{currentDay}. gün tüm çalışan maaşları ve mesai gideri"
                : $"{currentDay}. gün tüm çalışan maaşları";
            ApplyExpense(totalPayrollExpense, LedgerEntryType.PayrollExpense, description);
        }

        private void ProcessRecurringPayouts()
        {
            for (var i = 0; i < activeProjects.Count; i++)
            {
                var activeProject = activeProjects[i];
                while (activeProject.NextPayoutDay <= currentDay)
                {
                    var cycleCosts = activeProject.CyclePayrollCost + activeProject.CycleRecurringInvestmentCost;
                    if (Balance < cycleCosts)
                    {
                        var deficit = cycleCosts - Balance;
                        if (companyBankManager == null || !companyBankManager.TryAutoLoan(deficit))
                        {
                            lastExecutionSummary = $"{activeProject.DisplayName}: {currentDay}. günde döngü gideri için bakiye yetersiz.";
                            break;
                        }

                        if (Balance < cycleCosts)
                        {
                            lastExecutionSummary = $"{activeProject.DisplayName}: {currentDay}. günde alınan kredi döngü giderini karşılamadı.";
                            break;
                        }
                    }

                    ApplyExpense(activeProject.CyclePayrollCost, LedgerEntryType.PayrollExpense, $"{activeProject.DisplayName} dönemsel personel gideri");
                    ApplyExpense(activeProject.CycleRecurringInvestmentCost, LedgerEntryType.InvestmentExpense, $"{activeProject.DisplayName} dönemsel yatırım gideri");

                    var realizedRevenue = activeProject.RollCycleRevenue();
                    var competitionMultiplier = SectorCompetitionService.GetCachedRevenueMultiplier(activeProject.Sector);
                    var agentMultiplier = activeProject.IsAgentAffected ? activeProject.AgentRevenueReductionMultiplier : 1f;
                    realizedRevenue = Money.From(realizedRevenue.Amount * competitionMultiplier * agentMultiplier);
                    if (realizedRevenue > Money.Zero)
                    {
                        RecordIncomeInternal(realizedRevenue, LedgerEntryType.ProjectRevenue, $"{activeProject.DisplayName} dönemsel gelir");
                    }

                    executionHistory.Add(new ProjectExecutionHistoryEntry(activeProject.SourceDefinition, activeProject.DisplayName, activeProject.ProjectType, activeProject.CurrentResult));
                    var realizedProfit = realizedRevenue - cycleCosts;
                    lastExecutionSummary = $"{activeProject.DisplayName}: {currentDay}. günde dönemsel kâr {realizedProfit.Amount:N0}.";
                    activeProject.RegisterPayout();
                }
            }
        }

        private bool AreInvestmentAllocationsNonDecreasing(ActiveProjectRuntimeEntry activeProject, IReadOnlyList<InvestmentAllocationInput> newAllocations)
        {
            if (newAllocations == null)
            {
                return true;
            }

            for (var i = 0; i < newAllocations.Count; i++)
            {
                var allocation = newAllocations[i];
                if (allocation.InvestmentType == null)
                {
                    continue;
                }

                if (allocation.AllocatedBudgetAmount < activeProject.GetCurrentBudgetFor(allocation.InvestmentType))
                {
                    return false;
                }
            }

            return true;
        }

        private void UpdateSnapshot()
        {
            currentBalance = Balance.Amount;
        }

        private T[] ToArray<T>(IReadOnlyList<T> source)
        {
            var result = new T[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                result[i] = source[i];
            }

            return result;
        }
    }
}
