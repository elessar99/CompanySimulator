using System;
using System.Collections.Generic;
using CompanySimulator.Features.Accounting.Runtime.Components;
using CompanySimulator.Features.Banking.Runtime.Components;
using CompanySimulator.Features.Banking.Runtime.Models;
using CompanySimulator.Features.Employees.Runtime.Components;
using CompanySimulator.Features.Finance.Runtime.Components;
using CompanySimulator.Features.Finance.Runtime.Models;
using CompanySimulator.Features.FinanceOverview.Runtime.Models;
using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;

namespace CompanySimulator.Features.FinanceOverview.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class CompanyFinanceOverviewManager : MonoBehaviour
    {
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private CompanyAccountingManager companyAccountingManager;
        [SerializeField] private CompanyBankManager companyBankManager;
        [SerializeField] private EmployeeManager employeeManager;
        [SerializeField] private bool initializeOnAwake = true;

        private bool isInitialized;

        public event Action DataChanged;

        public bool IsInitialized => isInitialized;

        private void Awake()
        {
            economyManager ??= FindObjectOfType<EconomyManager>();
            companyAccountingManager ??= FindObjectOfType<CompanyAccountingManager>();
            companyBankManager ??= FindObjectOfType<CompanyBankManager>();
            employeeManager ??= FindObjectOfType<EmployeeManager>();
            if (initializeOnAwake)
            {
                Initialize();
            }
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        [ContextMenu("Finans Takip Sistemini Başlat")]
        public void Initialize()
        {
            economyManager ??= FindObjectOfType<EconomyManager>();
            companyAccountingManager ??= FindObjectOfType<CompanyAccountingManager>();
            companyBankManager ??= FindObjectOfType<CompanyBankManager>();
            employeeManager ??= FindObjectOfType<EmployeeManager>();

            if (economyManager != null && !economyManager.IsInitialized)
            {
                economyManager.Initialize();
            }

            if (companyAccountingManager != null && !companyAccountingManager.IsInitialized)
            {
                companyAccountingManager.Initialize();
            }

            if (companyBankManager != null && !companyBankManager.IsInitialized)
            {
                companyBankManager.Initialize();
            }

            if (employeeManager != null && !employeeManager.IsInitialized)
            {
                employeeManager.Initialize();
            }

            isInitialized = economyManager != null;
            DataChanged?.Invoke();
        }

        public FinanceDaySnapshot GetPreviousDaySnapshot()
        {
            var currentDay = economyManager != null ? economyManager.CurrentDay : 1;
            return BuildDaySnapshot(Mathf.Max(0, currentDay - 1));
        }

        public FinanceDaySnapshot GetCurrentDaySnapshot()
        {
            var currentDay = economyManager != null ? economyManager.CurrentDay : 1;
            return BuildDaySnapshot(currentDay);
        }

        public FinanceForecastSnapshot GetUpcomingIncomeSnapshot()
        {
            var items = new List<FinanceLineItemSnapshot>(8);
            var total = Money.Zero;
            var currentDay = economyManager != null ? economyManager.CurrentDay : 1;
            if (!EnsureInitialized() || economyManager == null)
            {
                return new FinanceForecastSnapshot(currentDay, items, total);
            }

            var activeProjects = economyManager.ActiveProjects;
            for (var i = 0; i < activeProjects.Count; i++)
            {
                var activeProject = activeProjects[i];
                if (activeProject == null)
                {
                    continue;
                }

                var detail = $"Beklenen Gün: {activeProject.NextPayoutDay}";
                var amount = activeProject.CompetitionAdjustedCycleRevenue;
                items.Add(new FinanceLineItemSnapshot(activeProject.DisplayName, amount, detail));
                total += amount;
            }

            return new FinanceForecastSnapshot(currentDay, items, total);
        }

        public FinanceForecastSnapshot GetNextDayIncomeSnapshot()
        {
            var items = new List<FinanceLineItemSnapshot>(8);
            var total = Money.Zero;
            var currentDay = economyManager != null ? economyManager.CurrentDay : 1;
            var nextDay = currentDay + 1;
            if (!EnsureInitialized() || economyManager == null)
            {
                return new FinanceForecastSnapshot(nextDay, items, total);
            }

            var activeProjects = economyManager.ActiveProjects;
            for (var i = 0; i < activeProjects.Count; i++)
            {
                var activeProject = activeProjects[i];
                if (activeProject == null || activeProject.NextPayoutDay != nextDay)
                {
                    continue;
                }

                items.Add(new FinanceLineItemSnapshot(activeProject.DisplayName, activeProject.CompetitionAdjustedCycleRevenue, $"{nextDay}. gün beklenen gelir"));
                total += activeProject.CompetitionAdjustedCycleRevenue;
            }

            return new FinanceForecastSnapshot(nextDay, items, total);
        }

        public FinanceForecastSnapshot GetNextDayPaymentSnapshot()
        {
            var items = new List<FinanceLineItemSnapshot>(8);
            var total = Money.Zero;
            var currentDay = economyManager != null ? economyManager.CurrentDay : 1;
            var nextDay = currentDay + 1;
            if (!EnsureInitialized())
            {
                return new FinanceForecastSnapshot(nextDay, items, total);
            }

            var dailyPayroll = GetDailyPayrollAmount();
            if (dailyPayroll > Money.Zero)
            {
                items.Add(new FinanceLineItemSnapshot("Günlük Maaşlar", dailyPayroll, $"{nextDay}. gün tüm çalışan maaşları"));
                total += dailyPayroll;
            }

            if (economyManager != null)
            {
                var activeProjects = economyManager.ActiveProjects;
                for (var i = 0; i < activeProjects.Count; i++)
                {
                    var activeProject = activeProjects[i];
                    if (activeProject == null || activeProject.NextPayoutDay != nextDay)
                    {
                        continue;
                    }

                    var cycleCost = activeProject.CyclePayrollCost + activeProject.CycleRecurringInvestmentCost;
                    if (cycleCost > Money.Zero)
                    {
                        items.Add(new FinanceLineItemSnapshot(activeProject.DisplayName, cycleCost, $"{nextDay}. gün dönemsel gider"));
                        total += cycleCost;
                    }
                }
            }

            if (companyBankManager != null)
            {
                var activeLoans = companyBankManager.ActiveLoans;
                for (var i = 0; i < activeLoans.Count; i++)
                {
                    var loan = activeLoans[i];
                    if (loan == null || loan.NextDueDay != nextDay)
                    {
                        continue;
                    }

                    var installment = loan.BuildCurrentInstallmentAmount();
                    items.Add(new FinanceLineItemSnapshot(loan.DisplayName, installment, $"{nextDay}. gün kredi taksiti"));
                    total += installment;
                }
            }

            if (companyAccountingManager != null)
            {
                var cycleSnapshot = companyAccountingManager.GetCurrentCycleSnapshot();
                if (cycleSnapshot.DaysUntilTaxPayment == 1 && cycleSnapshot.EstimatedTax > Money.Zero)
                {
                    items.Add(new FinanceLineItemSnapshot("Gelir Vergisi", cycleSnapshot.EstimatedTax, $"{nextDay}. gün vergi ödemesi"));
                    total += cycleSnapshot.EstimatedTax;
                }
            }

            return new FinanceForecastSnapshot(nextDay, items, total);
        }

        private FinanceDaySnapshot BuildDaySnapshot(int day)
        {
            var incomes = new List<FinanceLineItemSnapshot>(8);
            var expenses = new List<FinanceLineItemSnapshot>(8);
            var totalIncome = Money.Zero;
            var totalExpense = Money.Zero;
            if (!EnsureInitialized() || economyManager == null || day <= 0)
            {
                return new FinanceDaySnapshot(day, incomes, expenses, totalIncome, totalExpense);
            }

            var entries = economyManager.Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.Day != day)
                {
                    continue;
                }

                if (entry.Amount > Money.Zero)
                {
                    incomes.Add(new FinanceLineItemSnapshot(entry.Description, entry.Amount, GetLedgerTypeLabel(entry.Type)));
                    totalIncome += entry.Amount;
                }
                else if (entry.Amount < Money.Zero)
                {
                    var expenseAmount = -entry.Amount;
                    expenses.Add(new FinanceLineItemSnapshot(entry.Description, expenseAmount, GetLedgerTypeLabel(entry.Type)));
                    totalExpense += expenseAmount;
                }
            }

            return new FinanceDaySnapshot(day, incomes, expenses, totalIncome, totalExpense);
        }

        private Money GetDailyPayrollAmount()
        {
            if (employeeManager == null)
            {
                return Money.Zero;
            }

            var employees = employeeManager.Employees;
            var total = Money.Zero;
            for (var i = 0; i < employees.Count; i++)
            {
                total += employees[i].ExpectedDailySalary;
            }

            return total;
        }

        private string GetLedgerTypeLabel(LedgerEntryType type)
        {
            switch (type)
            {
                case LedgerEntryType.ProjectRevenue:
                    return "Proje Geliri";
                case LedgerEntryType.PayrollExpense:
                    return "Maaş Gideri";
                case LedgerEntryType.InvestmentExpense:
                    return "Yatırım Gideri";
                case LedgerEntryType.TaxExpense:
                    return "Vergi";
                case LedgerEntryType.LoanIncome:
                    return "Kredi Girişi";
                case LedgerEntryType.LoanRepaymentExpense:
                    return "Kredi Ödemesi";
                case LedgerEntryType.InitialCapital:
                    return "Başlangıç Sermayesi";
                case LedgerEntryType.ProjectSaleIncome:
                    return "İş Satış Geliri";
                default:
                    return type.ToString();
            }
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

        private void SubscribeEvents()
        {
            if (economyManager == null)
            {
                economyManager = FindObjectOfType<EconomyManager>();
            }

            if (companyAccountingManager == null)
            {
                companyAccountingManager = FindObjectOfType<CompanyAccountingManager>();
            }

            if (companyBankManager == null)
            {
                companyBankManager = FindObjectOfType<CompanyBankManager>();
            }

            if (employeeManager == null)
            {
                employeeManager = FindObjectOfType<EmployeeManager>();
            }

            if (economyManager != null)
            {
                economyManager.LedgerEntryRecorded -= HandleDataChanged;
                economyManager.LedgerEntryRecorded += HandleDataChanged;
                economyManager.DayAdvanced -= HandleDayAdvanced;
                economyManager.DayAdvanced += HandleDayAdvanced;
            }

            if (companyAccountingManager != null)
            {
                companyAccountingManager.DataChanged -= HandleSimpleDataChanged;
                companyAccountingManager.DataChanged += HandleSimpleDataChanged;
            }

            if (companyBankManager != null)
            {
                companyBankManager.DataChanged -= HandleSimpleDataChanged;
                companyBankManager.DataChanged += HandleSimpleDataChanged;
            }

            if (employeeManager != null)
            {
                employeeManager.DataChanged -= HandleSimpleDataChanged;
                employeeManager.DataChanged += HandleSimpleDataChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            if (economyManager != null)
            {
                economyManager.LedgerEntryRecorded -= HandleDataChanged;
                economyManager.DayAdvanced -= HandleDayAdvanced;
            }

            if (companyAccountingManager != null)
            {
                companyAccountingManager.DataChanged -= HandleSimpleDataChanged;
            }

            if (companyBankManager != null)
            {
                companyBankManager.DataChanged -= HandleSimpleDataChanged;
            }

            if (employeeManager != null)
            {
                employeeManager.DataChanged -= HandleSimpleDataChanged;
            }
        }

        private void HandleDataChanged(LedgerEntry _)
        {
            DataChanged?.Invoke();
        }

        private void HandleDayAdvanced(int _)
        {
            DataChanged?.Invoke();
        }

        private void HandleSimpleDataChanged()
        {
            DataChanged?.Invoke();
        }
    }
}
