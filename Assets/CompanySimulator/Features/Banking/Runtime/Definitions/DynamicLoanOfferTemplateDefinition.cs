using CompanySimulator.Shared.Runtime.Definitions;
using UnityEngine;

namespace CompanySimulator.Features.Banking.Runtime.Definitions
{
    [CreateAssetMenu(fileName = "DynamicLoanOfferTemplateDefinition", menuName = "Company Simulator/Definitions/Banking/Dynamic Loan Template")]
    public sealed class DynamicLoanOfferTemplateDefinition : DefinitionBase
    {
        [SerializeField, Min(0.1f)] private float monthlyRevenueMultiplier = 1f;
        [SerializeField, Range(0f, 5f)] private float minimumInterestRate = 0.12f;
        [SerializeField, Range(0f, 5f)] private float maximumInterestRate = 0.28f;
        [SerializeField, Min(0f)] private float balanceToLoanRatioForMinimumInterest = 2f;
        [SerializeField, Min(0.1f)] private float loanToBalanceRatioForMaximumInterest = 2f;
        [SerializeField, Min(1)] private int installmentIntervalDays = 5;
        [SerializeField, Min(1)] private int totalTermDays = 30;

        public float MonthlyRevenueMultiplier => Mathf.Max(0.1f, monthlyRevenueMultiplier);
        public float MinimumInterestRate => Mathf.Max(0f, minimumInterestRate);
        public float MaximumInterestRate => Mathf.Max(MinimumInterestRate, maximumInterestRate);
        public float BalanceToLoanRatioForMinimumInterest => Mathf.Max(0f, balanceToLoanRatioForMinimumInterest);
        public float LoanToBalanceRatioForMaximumInterest => Mathf.Max(0.1f, loanToBalanceRatioForMaximumInterest);
        public int InstallmentIntervalDays => Mathf.Max(1, installmentIntervalDays);
        public int TotalTermDays => Mathf.Max(1, totalTermDays);
    }
}
