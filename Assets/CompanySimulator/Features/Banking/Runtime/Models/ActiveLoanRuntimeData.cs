using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;

namespace CompanySimulator.Features.Banking.Runtime.Models
{
    public sealed class ActiveLoanRuntimeData
    {
        private readonly Money installmentAmount;
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

            remainingInstallmentCount = Mathf.Max(1, Mathf.CeilToInt(TotalTermDays / (float)InstallmentIntervalDays));
            var totalRepayment = Money.From(PrincipalAmount.Amount * (1d + InterestRate));
            installmentAmount = Money.From(totalRepayment.Amount / (double)remainingInstallmentCount);
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
        public Money InstallmentAmount => installmentAmount;
        public Money RemainingDebt { get; private set; }
        public bool IsClosed => remainingInstallmentCount <= 0 || RemainingDebt <= Money.Zero;

        public Money BuildCurrentInstallmentAmount()
        {
            return remainingInstallmentCount <= 1 ? RemainingDebt : installmentAmount;
        }

        public void RegisterInstallmentPayment(Money paidAmount)
        {
            RemainingDebt = RemainingDebt - paidAmount;
            remainingInstallmentCount = Mathf.Max(0, remainingInstallmentCount - 1);
            NextDueDay += InstallmentIntervalDays;
        }
    }
}
