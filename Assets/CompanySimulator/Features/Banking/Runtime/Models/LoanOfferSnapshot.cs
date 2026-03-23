using CompanySimulator.Shared.Runtime.Economy;

namespace CompanySimulator.Features.Banking.Runtime.Models
{
    public readonly struct LoanOfferSnapshot
    {
        public LoanOfferSnapshot(
            string offerId,
            string displayName,
            bool isSpecialOffer,
            Money principalAmount,
            float interestRate,
            int installmentIntervalDays,
            int totalTermDays,
            bool canAccept,
            string validationMessage)
        {
            OfferId = offerId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            IsSpecialOffer = isSpecialOffer;
            PrincipalAmount = principalAmount;
            InterestRate = interestRate;
            InstallmentIntervalDays = installmentIntervalDays;
            TotalTermDays = totalTermDays;
            CanAccept = canAccept;
            ValidationMessage = validationMessage ?? string.Empty;
        }

        public string OfferId { get; }
        public string DisplayName { get; }
        public bool IsSpecialOffer { get; }
        public Money PrincipalAmount { get; }
        public float InterestRate { get; }
        public int InstallmentIntervalDays { get; }
        public int TotalTermDays { get; }
        public bool CanAccept { get; }
        public string ValidationMessage { get; }
    }
}
