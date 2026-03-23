using System;
using System.Collections.Generic;
using CompanySimulator.Features.Accounting.Runtime.Models;
using CompanySimulator.Features.Employees.Runtime.Components;
using CompanySimulator.Features.Employees.Runtime.Definitions;
using CompanySimulator.Features.Employees.Runtime.Models;
using CompanySimulator.Features.Finance.Runtime.Components;
using CompanySimulator.Features.Finance.Runtime.Models;
using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;

namespace CompanySimulator.Features.Accounting.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class CompanyAccountingManager : MonoBehaviour
    {
        private const string AccountantAssignmentName = "Şirket Muhasebesi";

        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private EmployeeManager employeeManager;
        [SerializeField] private EmployeeRoleDefinition accountantRole;
        [SerializeField] private bool initializeOnAwake = true;
        [SerializeField, Min(1)] private int taxCycleLengthDays = 30;
        [SerializeField, Range(0f, 1f)] private float incomeTaxRate = 0.25f;
        [SerializeField, Min(1)] private int currentCycleStartDay = 1;
        [SerializeField] private long lastTaxPaymentAmount;

        private bool isInitialized;

        public event Action DataChanged;

        public bool IsInitialized => isInitialized;
        public EmployeeRoleDefinition AccountantRole => accountantRole;
        public int TaxCycleLengthDays => Mathf.Max(1, taxCycleLengthDays);
        public float IncomeTaxRate => Mathf.Clamp01(incomeTaxRate);
        public int ActiveProjectCount => economyManager != null ? economyManager.ActiveProjects.Count : 0;
        public int MaxActiveProjectCount => CalculateCapacity(GetAssignedAccountantsInternal());

        private void Awake()
        {
            economyManager ??= FindObjectOfType<EconomyManager>();
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

        [ContextMenu("Muhasebe Sistemini Başlat")]
        public void Initialize()
        {
            economyManager ??= FindObjectOfType<EconomyManager>();
            employeeManager ??= FindObjectOfType<EmployeeManager>();

            if (economyManager != null && !economyManager.IsInitialized)
            {
                economyManager.Initialize();
            }

            if (employeeManager != null && !employeeManager.IsInitialized)
            {
                employeeManager.Initialize();
            }

            ResolveAccountantRole();
            currentCycleStartDay = Mathf.Max(1, economyManager != null ? economyManager.CurrentDay : currentCycleStartDay);
            lastTaxPaymentAmount = 0L;
            isInitialized = true;
            DataChanged?.Invoke();
        }

        public IReadOnlyList<EmployeeRuntimeData> GetAssignedAccountants()
        {
            return GetAssignedAccountantsInternal();
        }

        public IReadOnlyList<EmployeeRuntimeData> GetAvailableAccountants()
        {
            var result = new List<EmployeeRuntimeData>(8);
            if (!EnsureInitialized() || employeeManager == null || accountantRole == null)
            {
                return result;
            }

            var employees = employeeManager.GetEmployeesByRole(accountantRole);
            for (var i = 0; i < employees.Count; i++)
            {
                var employee = employees[i];
                if (employee != null && !employee.IsAssigned)
                {
                    result.Add(employee);
                }
            }

            return result;
        }

        public AccountingCycleSnapshot GetCurrentCycleSnapshot()
        {
            var income = Money.Zero;
            var expenses = Money.Zero;
            var currentDay = economyManager != null ? economyManager.CurrentDay : currentCycleStartDay;
            var nextTaxDay = currentCycleStartDay + TaxCycleLengthDays - 1;
            var effectiveDay = Mathf.Max(currentDay, currentCycleStartDay);

            if (economyManager != null)
            {
                var entries = economyManager.Entries;
                for (var i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    if (entry.Day < currentCycleStartDay)
                    {
                        continue;
                    }

                    if (IsTrackedIncome(entry.Type) && entry.Amount > Money.Zero)
                    {
                        income += entry.Amount;
                        continue;
                    }

                    if (IsTrackedExpense(entry.Type) && entry.Amount < Money.Zero)
                    {
                        expenses += -entry.Amount;
                    }
                }
            }

            var profit = income - expenses;
            var estimatedTax = profit > Money.Zero ? Money.From(profit.Amount * IncomeTaxRate) : Money.Zero;
            var daysUntilTaxPayment = Mathf.Max(0, nextTaxDay - effectiveDay + 1);
            return new AccountingCycleSnapshot(
                currentCycleStartDay,
                nextTaxDay,
                daysUntilTaxPayment,
                ActiveProjectCount,
                MaxActiveProjectCount,
                income,
                expenses,
                profit,
                estimatedTax,
                Money.From(lastTaxPaymentAmount));
        }

        public bool CanCreateAdditionalProject(out string validationMessage)
        {
            validationMessage = string.Empty;
            if (!EnsureInitialized())
            {
                validationMessage = "Muhasebe sistemi hazır değil.";
                return false;
            }

            if (accountantRole == null)
            {
                validationMessage = "Muhasebeçi rolü bulunamadı. En az bir muhasebeçi eklemelisin.";
                return false;
            }

            var capacity = MaxActiveProjectCount;
            if (capacity <= 0)
            {
                validationMessage = "Yeni iş başlatmak için şirkete en az bir muhasebeçi atamalısın.";
                return false;
            }

            if (ActiveProjectCount >= capacity)
            {
                validationMessage = $"Aktif iş kapasitesi dolu. Mevcut kapasite: {capacity}.";
                return false;
            }

            return true;
        }

        public bool TryAssignAccountant(EmployeeRuntimeData accountant, out string validationMessage)
        {
            validationMessage = string.Empty;
            if (!EnsureInitialized())
            {
                validationMessage = "Muhasebe sistemi hazır değil.";
                return false;
            }

            if (!IsAccountantCandidate(accountant))
            {
                validationMessage = "Seçilen çalışan muhasebeçi değil.";
                return false;
            }

            if (accountant.IsAssigned)
            {
                validationMessage = "Seçilen muhasebeçi şu anda başka bir görevde çalışıyor.";
                return false;
            }

            if (employeeManager == null || !employeeManager.TryAssignEmployees(new[] { accountant }, AccountantAssignmentName))
            {
                validationMessage = "Muhasebeçi şirkete atanamadı.";
                return false;
            }

            DataChanged?.Invoke();
            return true;
        }

        public bool TryUnassignAccountant(EmployeeRuntimeData accountant, out string validationMessage)
        {
            if (!CanUnassignAccountant(accountant, out validationMessage))
            {
                return false;
            }

            if (employeeManager == null || !employeeManager.TryClearAssignment(accountant, AccountantAssignmentName))
            {
                validationMessage = "Muhasebeçi şirket görevinden ayrılamadı.";
                return false;
            }

            DataChanged?.Invoke();
            return true;
        }

        public bool CanUnassignAccountant(EmployeeRuntimeData accountant, out string validationMessage)
        {
            validationMessage = string.Empty;
            if (!EnsureInitialized())
            {
                validationMessage = "Muhasebe sistemi hazır değil.";
                return false;
            }

            if (accountant == null || !IsAssignedAccountant(accountant))
            {
                validationMessage = "Seçilen çalışan şirkete atanmış bir muhasebeçi değil.";
                return false;
            }

            var capacityAfterRemoval = MaxActiveProjectCount - GetCapacityContribution(accountant);
            if (capacityAfterRemoval < ActiveProjectCount)
            {
                validationMessage = $"Bu muhasebeçiyi ayırırsan kapasite {capacityAfterRemoval} olur ve mevcut aktif iş sayısı {ActiveProjectCount} altında kalır.";
                return false;
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

        private void SubscribeEvents()
        {
            if (economyManager == null)
            {
                economyManager = FindObjectOfType<EconomyManager>();
            }

            if (employeeManager == null)
            {
                employeeManager = FindObjectOfType<EmployeeManager>();
            }

            if (economyManager != null)
            {
                economyManager.DayAdvanced -= HandleDayAdvanced;
                economyManager.DayAdvanced += HandleDayAdvanced;
                economyManager.LedgerEntryRecorded -= HandleLedgerEntryRecorded;
                economyManager.LedgerEntryRecorded += HandleLedgerEntryRecorded;
            }

            if (employeeManager != null)
            {
                employeeManager.DataChanged -= HandleEmployeeDataChanged;
                employeeManager.DataChanged += HandleEmployeeDataChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            if (economyManager != null)
            {
                economyManager.DayAdvanced -= HandleDayAdvanced;
                economyManager.LedgerEntryRecorded -= HandleLedgerEntryRecorded;
            }

            if (employeeManager != null)
            {
                employeeManager.DataChanged -= HandleEmployeeDataChanged;
            }
        }

        private void HandleDayAdvanced(int _)
        {
            TryProcessTaxPayment();
            DataChanged?.Invoke();
        }

        private void HandleLedgerEntryRecorded(LedgerEntry _)
        {
            DataChanged?.Invoke();
        }

        private void HandleEmployeeDataChanged()
        {
            ResolveAccountantRole();
            DataChanged?.Invoke();
        }

        private void TryProcessTaxPayment()
        {
            if (!EnsureInitialized() || economyManager == null)
            {
                return;
            }

            var snapshot = GetCurrentCycleSnapshot();
            if (snapshot.DaysUntilTaxPayment > 0 || snapshot.EstimatedTax <= Money.Zero)
            {
                return;
            }

            if (!economyManager.TryRecordExpense(snapshot.EstimatedTax, LedgerEntryType.TaxExpense, $"{TaxCycleLengthDays} günlük gelir vergisi"))
            {
                return;
            }

            lastTaxPaymentAmount = snapshot.EstimatedTax.Amount;
            currentCycleStartDay = economyManager.CurrentDay + 1;
        }

        private void ResolveAccountantRole()
        {
            if (employeeManager == null)
            {
                return;
            }

            if (!employeeManager.IsInitialized)
            {
                employeeManager.Initialize();
            }

            if (accountantRole != null)
            {
                return;
            }

            var roles = employeeManager.Roles;
            for (var i = 0; i < roles.Count; i++)
            {
                var role = roles[i];
                if (role == null)
                {
                    continue;
                }

                var normalizedId = NormalizeToken(role.Id);
                var normalizedName = NormalizeToken(role.DisplayName);
                if (normalizedId == "muhasebeci" || normalizedName == "muhasebeci")
                {
                    accountantRole = role;
                    return;
                }
            }
        }

        private IReadOnlyList<EmployeeRuntimeData> GetAssignedAccountantsInternal()
        {
            var result = new List<EmployeeRuntimeData>(8);
            if (!EnsureInitialized() || employeeManager == null || accountantRole == null)
            {
                return result;
            }

            var employees = employeeManager.GetEmployeesByRole(accountantRole);
            for (var i = 0; i < employees.Count; i++)
            {
                if (IsAssignedAccountant(employees[i]))
                {
                    result.Add(employees[i]);
                }
            }

            return result;
        }

        private bool IsAssignedAccountant(EmployeeRuntimeData employee)
        {
            return employee != null && IsAccountantCandidate(employee) && string.Equals(employee.CurrentAssignmentName, AccountantAssignmentName, StringComparison.Ordinal);
        }

        private bool IsAccountantCandidate(EmployeeRuntimeData employee)
        {
            return employee != null && accountantRole != null && employee.Role == accountantRole;
        }

        private int CalculateCapacity(IReadOnlyList<EmployeeRuntimeData> accountants)
        {
            var totalCapacity = 0;
            for (var i = 0; i < accountants.Count; i++)
            {
                totalCapacity += GetCapacityContribution(accountants[i]);
            }

            return totalCapacity;
        }

        private int GetCapacityContribution(EmployeeRuntimeData accountant)
        {
            return accountant != null ? Mathf.Max(1, Mathf.CeilToInt(accountant.IncomeMultiplier)) : 0;
        }

        private bool IsTrackedIncome(LedgerEntryType type)
        {
            return type == LedgerEntryType.ProjectRevenue || type == LedgerEntryType.MiscIncome;
        }

        private bool IsTrackedExpense(LedgerEntryType type)
        {
            return type == LedgerEntryType.PayrollExpense
                || type == LedgerEntryType.InvestmentExpense
                || type == LedgerEntryType.RentExpense
                || type == LedgerEntryType.MiscExpense;
        }

        private string NormalizeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim().ToLowerInvariant()
                .Replace('ç', 'c')
                .Replace('ğ', 'g')
                .Replace('ı', 'i')
                .Replace('ö', 'o')
                .Replace('ş', 's')
                .Replace('ü', 'u');
        }
    }
}
