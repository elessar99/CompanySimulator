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
        [SerializeField] private long currentBalance;
        [SerializeField] private int executedProjectCount;
        [SerializeField] private string lastExecutionSummary;

        private CompanyLedger ledger;
        private ProjectEconomyCalculator calculator;
        private readonly List<ProjectExecutionHistoryEntry> executionHistory = new List<ProjectExecutionHistoryEntry>(32);
        private bool isInitialized;

        public event Action<Money> BalanceChanged;
        public event Action<ProjectExecutionDefinition, ProjectEconomyResult> ProjectExecuted;
        public event Action<ProjectExecutionDefinition, ProjectEconomyResult> ProjectExecutionRejected;

        public bool IsInitialized => isInitialized;
        public Money Balance => ledger != null ? ledger.Balance : Money.Zero;
        public IReadOnlyList<LedgerEntry> Entries => ledger != null ? ledger.Entries : Array.Empty<LedgerEntry>();
        public IReadOnlyList<ProjectExecutionHistoryEntry> ExecutionHistory => executionHistory;
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

            return calculator.Calculate(executionDefinition.CreateRequest());
        }

        public bool CanExecuteProject(ProjectExecutionDefinition executionDefinition, out ProjectEconomyResult result)
        {
            result = PreviewProject(executionDefinition);
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

            result = calculator.Calculate(executionDefinition.CreateRequest());
            if (Balance < result.TotalCosts)
            {
                lastExecutionSummary = $"{executionDefinition.DisplayName}: yetersiz bakiye.";
                UpdateSnapshot();
                ProjectExecutionRejected?.Invoke(executionDefinition, result);
                return false;
            }

            ApplyExpense(result.FixedCost, LedgerEntryType.MiscExpense, $"{executionDefinition.DisplayName} sabit gider");
            ApplyExpense(result.PayrollCost, LedgerEntryType.PayrollExpense, $"{executionDefinition.DisplayName} personel gideri");
            ApplyExpense(result.InvestmentCost, LedgerEntryType.InvestmentExpense, $"{executionDefinition.DisplayName} yatırım gideri");

            if (result.Revenue > Money.Zero)
            {
                ledger.RecordIncome(result.Revenue, LedgerEntryType.ProjectRevenue, executionDefinition.DisplayName);
            }

            // Geçmiş kayıtları sektör paneli gibi ekranların tamamlanan işleri sayabilmesi için tutulur.
            executionHistory.Add(new ProjectExecutionHistoryEntry(executionDefinition, result));
            executedProjectCount++;
            lastExecutionSummary = $"{executionDefinition.DisplayName}: kâr {result.Profit.Amount}, başarı {result.SuccessScore:0.00}.";
            UpdateSnapshot();
            BalanceChanged?.Invoke(Balance);
            ProjectExecuted?.Invoke(executionDefinition, result);
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

        private void UpdateSnapshot()
        {
            currentBalance = Balance.Amount;
        }
    }
}
