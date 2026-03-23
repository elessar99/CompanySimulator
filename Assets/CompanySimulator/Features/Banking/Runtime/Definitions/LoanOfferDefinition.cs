using CompanySimulator.Shared.Runtime.Definitions;
using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;

namespace CompanySimulator.Features.Banking.Runtime.Definitions
{
    [CreateAssetMenu(fileName = "LoanOfferDefinition", menuName = "Company Simulator/Definitions/Banking/Loan Offer")]
    public sealed class LoanOfferDefinition : DefinitionBase
    {
        [SerializeField, Min(0)] private int principalAmount = 250000;
        [SerializeField, Range(0f, 5f)] private float interestRate = 0.18f;
        [SerializeField, Min(1)] private int installmentIntervalDays = 5;
        [SerializeField, Min(1)] private int totalTermDays = 30;

        public Money PrincipalAmount => Money.From(principalAmount);
        public float InterestRate => Mathf.Max(0f, interestRate);
        public int InstallmentIntervalDays => Mathf.Max(1, installmentIntervalDays);
        public int TotalTermDays => Mathf.Max(1, totalTermDays);
    }
}
