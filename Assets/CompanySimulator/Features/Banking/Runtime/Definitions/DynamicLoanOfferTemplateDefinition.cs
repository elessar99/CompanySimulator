using CompanySimulator.Shared.Runtime.Definitions;
using UnityEngine;

namespace CompanySimulator.Features.Banking.Runtime.Definitions
{
    [CreateAssetMenu(fileName = "DynamicLoanOfferTemplateDefinition", menuName = "Company Simulator/Definitions/Banking/Dynamic Loan Template")]
    public sealed class DynamicLoanOfferTemplateDefinition : DefinitionBase
    {
        [SerializeField, Min(0.1f)] private float monthlyRevenueMultiplier = 1f;
        [SerializeField, Min(1)] private int installmentIntervalDays = 5;
        [SerializeField, Min(1)] private int totalTermDays = 30;

        public float MonthlyRevenueMultiplier => Mathf.Max(0.1f, monthlyRevenueMultiplier);
        public int InstallmentIntervalDays => Mathf.Max(1, installmentIntervalDays);
        public int TotalTermDays => Mathf.Max(1, totalTermDays);
    }
}
