using System;
using System.Collections.Generic;
using CompanySimulator.Features.Banking.Runtime.Definitions;
using CompanySimulator.Features.Banking.Runtime.Models;
using CompanySimulator.Features.Finance.Runtime.Components;
using CompanySimulator.Features.Finance.Runtime.Models;
using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;

namespace CompanySimulator.Features.Banking.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class CompanyBankManager : MonoBehaviour
    {
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private BankingSetupDefinition setup;
        [SerializeField] private bool initializeOnAwake = true;

        private readonly List<ActiveLoanRuntimeData> activeLoans = new List<ActiveLoanRuntimeData>(8);
        private bool isInitialized;
        private string lastBankSummary;

        public event Action DataChanged;

        public bool IsInitialized => isInitialized;
        public string LastBankSummary => lastBankSummary;
        public IReadOnlyList<ActiveLoanRuntimeData> ActiveLoans => activeLoans;

        private void Awake()
        {
            economyManager ??= FindObjectOfType<EconomyManager>();
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

        [ContextMenu("Banka Sistemini Başlat")]
        public void Initialize()
        {
            economyManager ??= FindObjectOfType<EconomyManager>();
            if (economyManager != null && !economyManager.IsInitialized)
            {
                economyManager.Initialize();
            }

            isInitialized = true;
            lastBankSummary = string.Empty;
            DataChanged?.Invoke();
        }

        public IReadOnlyList<LoanOfferSnapshot> GetStandardOffers()
        {
            var result = new List<LoanOfferSnapshot>(8);
            if (!EnsureInitialized() || setup == null)
            {
                return result;
            }

            var offers = setup.StandardOffers;
            for (var i = 0; i < offers.Count; i++)
            {
                var definition = offers[i];
                if (definition == null)
                {
                    continue;
                }

                var canAccept = !HasActiveStandardLoan(definition.Id);
                var validationMessage = canAccept ? string.Empty : "Bu standart kredi zaten aktif. Bitmeden tekrar alınamaz.";
                result.Add(new LoanOfferSnapshot(
                    definition.Id,
                    definition.DisplayName,
                    false,
                    definition.PrincipalAmount,
                    definition.InterestRate,
                    definition.InstallmentIntervalDays,
                    definition.TotalTermDays,
                    canAccept,
                    validationMessage));
            }

            return result;
        }

        public IReadOnlyList<LoanOfferSnapshot> GetSpecialOffers()
        {
            var result = new List<LoanOfferSnapshot>(8);
            if (!EnsureInitialized() || setup == null)
            {
                return result;
            }

            var templates = setup.SpecialOfferTemplates;
            var monthlyRevenue = GetMonthlyActiveProjectRevenue();
            var interestRate = ResolveSpecialInterestRate(monthlyRevenue);
            for (var i = 0; i < templates.Count; i++)
            {
                var template = templates[i];
                if (template == null)
                {
                    continue;
                }

                var principalAmount = BuildSpecialOfferAmount(monthlyRevenue, template.MonthlyRevenueMultiplier);
                result.Add(new LoanOfferSnapshot(
                    template.Id,
                    template.DisplayName,
                    true,
                    principalAmount,
                    interestRate,
                    template.InstallmentIntervalDays,
                    template.TotalTermDays,
                    principalAmount > Money.Zero,
                    principalAmount > Money.Zero ? string.Empty : "Özel kredi için aktif iş geliri bulunmuyor."));
            }

            return result;
        }

        public bool TryAcceptOffer(LoanOfferSnapshot offer, out string validationMessage)
        {
            validationMessage = string.Empty;
            if (!EnsureInitialized())
            {
                validationMessage = "Banka sistemi hazır değil.";
                return false;
            }

            if (!offer.CanAccept || offer.PrincipalAmount <= Money.Zero)
            {
                validationMessage = string.IsNullOrWhiteSpace(offer.ValidationMessage) ? "Bu kredi teklifi şu anda alınamaz." : offer.ValidationMessage;
                return false;
            }

            if (!offer.IsSpecialOffer && HasActiveStandardLoan(offer.OfferId))
            {
                validationMessage = "Bu standart kredi zaten aktif.";
                return false;
            }

            if (economyManager == null)
            {
                validationMessage = "Ekonomi sistemi bulunamadı.";
                return false;
            }

            economyManager.RecordIncome(offer.PrincipalAmount, LedgerEntryType.LoanIncome, $"{offer.DisplayName} kredi ödemesi");
            activeLoans.Add(new ActiveLoanRuntimeData(
                offer.OfferId,
                offer.DisplayName,
                offer.IsSpecialOffer,
                offer.PrincipalAmount,
                offer.InterestRate,
                offer.InstallmentIntervalDays,
                offer.TotalTermDays,
                economyManager.CurrentDay));
            lastBankSummary = $"{offer.DisplayName} kredisi alındı: {offer.PrincipalAmount.Amount:N0}";
            DataChanged?.Invoke();
            return true;
        }

        public Money GetMonthlyActiveProjectRevenue()
        {
            if (economyManager == null)
            {
                return Money.Zero;
            }

            var total = 0d;
            var activeProjects = economyManager.ActiveProjects;
            for (var i = 0; i < activeProjects.Count; i++)
            {
                var activeProject = activeProjects[i];
                if (activeProject == null || activeProject.PayoutIntervalDays <= 0)
                {
                    continue;
                }

                total += activeProject.CycleRevenue.Amount * (30d / activeProject.PayoutIntervalDays);
            }

            return Money.From(total);
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

            if (economyManager != null)
            {
                economyManager.DayAdvanced -= HandleDayAdvanced;
                economyManager.DayAdvanced += HandleDayAdvanced;
                economyManager.BalanceChanged -= HandleBalanceChanged;
                economyManager.BalanceChanged += HandleBalanceChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            if (economyManager != null)
            {
                economyManager.DayAdvanced -= HandleDayAdvanced;
                economyManager.BalanceChanged -= HandleBalanceChanged;
            }
        }

        private void HandleDayAdvanced(int _)
        {
            ProcessLoanInstallments();
            DataChanged?.Invoke();
        }

        private void HandleBalanceChanged(Money _)
        {
            DataChanged?.Invoke();
        }

        private void ProcessLoanInstallments()
        {
            if (!EnsureInitialized() || economyManager == null)
            {
                return;
            }

            for (var i = activeLoans.Count - 1; i >= 0; i--)
            {
                var loan = activeLoans[i];
                while (loan.NextDueDay <= economyManager.CurrentDay && !loan.IsClosed)
                {
                    var installmentAmount = loan.BuildCurrentInstallmentAmount();
                    if (!economyManager.TryRecordExpense(installmentAmount, LedgerEntryType.LoanRepaymentExpense, $"{loan.DisplayName} taksit ödemesi"))
                    {
                        lastBankSummary = $"{loan.DisplayName} için {economyManager.CurrentDay}. gündeki taksit ödenemedi.";
                        break;
                    }

                    loan.RegisterInstallmentPayment(installmentAmount);
                    lastBankSummary = $"{loan.DisplayName} taksiti ödendi: {installmentAmount.Amount:N0}";
                }

                if (loan.IsClosed)
                {
                    activeLoans.RemoveAt(i);
                }
            }
        }

        private bool HasActiveStandardLoan(string offerId)
        {
            for (var i = 0; i < activeLoans.Count; i++)
            {
                var loan = activeLoans[i];
                if (!loan.IsSpecialOffer && string.Equals(loan.OfferId, offerId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private Money BuildSpecialOfferAmount(Money monthlyRevenue, float multiplier)
        {
            if (monthlyRevenue <= Money.Zero)
            {
                return Money.Zero;
            }

            var calculatedAmount = Money.From(monthlyRevenue.Amount * multiplier);
            return calculatedAmount > Money.Zero
                ? calculatedAmount
                : Money.From(setup != null ? setup.MinimumSpecialOfferAmount : 0);
        }

        private float ResolveSpecialInterestRate(Money monthlyRevenue)
        {
            if (setup == null)
            {
                return 0f;
            }

            if (economyManager == null)
            {
                return setup.BaseSpecialInterestRate;
            }

            var monthlyRevenueAmount = Math.Max(1d, monthlyRevenue.Amount);
            var balanceRatio = economyManager.Balance.Amount / monthlyRevenueAmount;
            var interestRate = ResolveReferenceStandardInterestRate();
            if (balanceRatio <= setup.LowBalanceToRevenueRatio)
            {
                interestRate += setup.PoorCompanyInterestOffset;
            }
            else if (balanceRatio >= setup.HighBalanceToRevenueRatio)
            {
                interestRate += setup.WealthyCompanyInterestOffset;
            }

            return Mathf.Max(0f, interestRate);
        }

        private float ResolveReferenceStandardInterestRate()
        {
            if (setup == null)
            {
                return 0f;
            }

            var offers = setup.StandardOffers;
            if (offers.Count <= 0)
            {
                return setup.BaseSpecialInterestRate;
            }

            var totalInterest = 0f;
            var count = 0;
            for (var i = 0; i < offers.Count; i++)
            {
                if (offers[i] == null)
                {
                    continue;
                }

                totalInterest += offers[i].InterestRate;
                count++;
            }

            return count > 0 ? totalInterest / count : setup.BaseSpecialInterestRate;
        }
    }
}
