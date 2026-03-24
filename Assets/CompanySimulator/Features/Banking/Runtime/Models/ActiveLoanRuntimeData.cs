using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;

namespace CompanySimulator.Features.Banking.Runtime.Models
{
    public sealed class ActiveLoanRuntimeData
    {
        private readonly int initialInstallmentCount;
        private readonly Money scheduledPrincipalInstallmentAmount;
        private int remainingInstallmentCount;

        public ActiveLoanRuntimeData(
            string offerId,
            string displayName,
            bool isSpecialOffer,
            Money principalAmount,
            float interestRate,
            int installmentIntervalDays,
            int totalTermDays,
            int startedDay)
        {
            OfferId = offerId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            IsSpecialOffer = isSpecialOffer;
            PrincipalAmount = principalAmount;
            InterestRate = Mathf.Max(0f, interestRate);
            InstallmentIntervalDays = Mathf.Max(1, installmentIntervalDays);
            TotalTermDays = Mathf.Max(1, totalTermDays);
            StartedDay = startedDay;

            initialInstallmentCount = Mathf.Max(1, Mathf.CeilToInt(TotalTermDays / (float)InstallmentIntervalDays));
            remainingInstallmentCount = initialInstallmentCount;
            scheduledPrincipalInstallmentAmount = Money.From(PrincipalAmount.Amount / (double)initialInstallmentCount);
            RemainingPrincipalAmount = PrincipalAmount;
            var totalRepayment = Money.From(PrincipalAmount.Amount * (1d + InterestRate));
            RemainingDebt = totalRepayment;
            NextDueDay = startedDay + InstallmentIntervalDays;
        }

        public string OfferId { get; }
        public string DisplayName { get; }
        public bool IsSpecialOffer { get; }
        public Money PrincipalAmount { get; }
        public float InterestRate { get; }
        public int InstallmentIntervalDays { get; }
        public int TotalTermDays { get; }
        public int StartedDay { get; }
        public int NextDueDay { get; private set; }
        public int RemainingInstallmentCount => remainingInstallmentCount;
        public Money RemainingPrincipalAmount { get; private set; }
        public Money RemainingDebt { get; private set; }
        public bool IsClosed => remainingInstallmentCount <= 0 || RemainingDebt <= Money.Zero;

        public Money BuildCurrentInstallmentAmount()
        {
            if (IsClosed)
            {
                return Money.Zero;
            }

            var principalInstallment = BuildCurrentPrincipalInstallment();
            var interestAmount = Money.From(RemainingPrincipalAmount.Amount * GetSinglePeriodInterestRate());
            return principalInstallment + interestAmount;
        }

        public Money GetEarlyClosureAmount()
        {
            if (IsClosed)
            {
                return Money.Zero;
            }

            var singlePeriodInterest = Money.From(RemainingPrincipalAmount.Amount * GetSinglePeriodInterestRate());
            return RemainingPrincipalAmount + singlePeriodInterest;
        }

        public void RegisterInstallmentPayment()
        {
            if (IsClosed)
            {
                return;
            }

            var paidAmount = BuildCurrentInstallmentAmount();
            var principalInstallment = BuildCurrentPrincipalInstallment();
            RemainingPrincipalAmount = RemainingPrincipalAmount - principalInstallment;
            RemainingDebt = RemainingDebt - paidAmount;
            if (RemainingDebt < Money.Zero)
            {
                RemainingDebt = Money.Zero;
            }

            remainingInstallmentCount = Mathf.Max(0, remainingInstallmentCount - 1);
            NextDueDay += InstallmentIntervalDays;
        }

        public void RegisterEarlyClosure()
        {
            RemainingPrincipalAmount = Money.Zero;
            RemainingDebt = Money.Zero;
            remainingInstallmentCount = 0;
        }

        private Money BuildCurrentPrincipalInstallment()
        {
            return remainingInstallmentCount <= 1 ? RemainingPrincipalAmount : scheduledPrincipalInstallmentAmount;
        }

        private float GetSinglePeriodInterestRate()
        {
            return initialInstallmentCount > 0 ? InterestRate / initialInstallmentCount : InterestRate;
        }
    }
}
