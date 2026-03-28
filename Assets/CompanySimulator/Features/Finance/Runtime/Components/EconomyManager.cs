using System;
using System.Collections.Generic;
using CompanySimulator.Features.Employees.Runtime.Components;
using CompanySimulator.Features.Employees.Runtime.Models;
using CompanySimulator.Features.Finance.Runtime.Definitions;
using CompanySimulator.Features.Finance.Runtime.Models;
using CompanySimulator.Features.Finance.Runtime.Services;
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
            var adjustedRevenue = Money.From(activeProject.CycleRevenue.Amount * competitionMultiplier);
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

        private bool EnsureInitialized()
        {
            if (isInitialized)
            {
                return true;
            }

            Initialize();
            return isInitialized;
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

            var employees = employeeManager.Employees;
            var totalDailyPayroll = Money.Zero;
            for (var i = 0; i < employees.Count; i++)
            {
                totalDailyPayroll += employees[i].ExpectedDailySalary;
            }

            if (totalDailyPayroll <= Money.Zero)
            {
                return;
            }

            if (Balance < totalDailyPayroll)
            {
                lastExecutionSummary = $"{currentDay}. günde günlük maaş gideri için bakiye yetersiz.";
                return;
            }

            ApplyExpense(totalDailyPayroll, LedgerEntryType.PayrollExpense, $"{currentDay}. gün tüm çalışan maaşları");
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
                        lastExecutionSummary = $"{activeProject.DisplayName}: {currentDay}. günde döngü gideri için bakiye yetersiz.";
                        break;
                    }

                    ApplyExpense(activeProject.CyclePayrollCost, LedgerEntryType.PayrollExpense, $"{activeProject.DisplayName} dönemsel personel gideri");
                    ApplyExpense(activeProject.CycleRecurringInvestmentCost, LedgerEntryType.InvestmentExpense, $"{activeProject.DisplayName} dönemsel yatırım gideri");

                    var realizedRevenue = activeProject.RollCycleRevenue();
                    var competitionMultiplier = SectorCompetitionService.GetCachedRevenueMultiplier(activeProject.Sector);
                    realizedRevenue = Money.From(realizedRevenue.Amount * competitionMultiplier);
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

        private string[] ToArray(IReadOnlyList<string> source)
        {
            var result = new string[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                result[i] = source[i];
            }

            return result;
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
