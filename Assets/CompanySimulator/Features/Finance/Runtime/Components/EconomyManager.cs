using System;
using System.Collections.Generic;
using CompanySimulator.Features.Finance.Runtime.Definitions;
using CompanySimulator.Features.Finance.Runtime.Models;
using CompanySimulator.Features.Finance.Runtime.Services;
using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;

namespace CompanySimulator.Features.Finance.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class EconomyManager : MonoBehaviour
    {
        [SerializeField] private EconomySetupDefinition setup;
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
        public event Action<ProjectExecutionDefinition, ProjectEconomyResult> ProjectExecuted;
        public event Action<ProjectExecutionDefinition, ProjectEconomyResult> ProjectExecutionRejected;

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
                ledger.RecordIncome(setup.StartingCapital, LedgerEntryType.InitialCapital, "Başlangıç sermayesi");
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

            return TryExecuteProject(executionDefinition, executionDefinition.CreateRequest(), executionDefinition.DisplayName, out result);
        }

        public bool TryExecuteProject(ProjectExecutionDefinition sourceDefinition, ProjectEconomyRequest request, string displayName, out ProjectEconomyResult result)
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
            var activeProject = new ActiveProjectRuntimeEntry(sourceDefinition, safeDisplayName, request.ProjectType, result, currentDay);
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

            if (!ledger.TryRecordExpense(amount, type, description))
            {
                throw new InvalidOperationException("Bakiye kontrolü geçildiği halde gider uygulanamadı.");
            }
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

                    if (activeProject.CycleRevenue > Money.Zero)
                    {
                        ledger.RecordIncome(activeProject.CycleRevenue, LedgerEntryType.ProjectRevenue, $"{activeProject.DisplayName} dönemsel gelir");
                    }

                    executionHistory.Add(new ProjectExecutionHistoryEntry(activeProject.SourceDefinition, activeProject.DisplayName, activeProject.ProjectType, activeProject.StartupResult));
                    lastExecutionSummary = $"{activeProject.DisplayName}: {currentDay}. günde dönemsel kâr {activeProject.CycleProfit.Amount:N0}.";
                    activeProject.RegisterPayout();
                }
            }
        }

        private void UpdateSnapshot()
        {
            currentBalance = Balance.Amount;
        }
    }
}
