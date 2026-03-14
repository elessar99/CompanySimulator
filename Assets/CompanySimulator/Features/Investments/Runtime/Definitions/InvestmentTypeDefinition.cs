using CompanySimulator.Features.Finance.Runtime.Definitions;
using CompanySimulator.Shared.Runtime.Definitions;
using UnityEngine;

namespace CompanySimulator.Features.Investments.Runtime.Definitions
{
    [CreateAssetMenu(fileName = "InvestmentTypeDefinition", menuName = "Company Simulator/Definitions/Investments/Investment Type")]
    public sealed class InvestmentTypeDefinition : DefinitionBase
    {
        [SerializeField] private BudgetResponseCurveDefinition budgetResponseCurve;
        [SerializeField, Min(0f)] private float profitWeight = 1f;
        [SerializeField, Min(0f)] private float successWeight = 1f;
        [SerializeField, Min(0)] private int recommendedBudget = 100000;

        public BudgetResponseCurveDefinition BudgetResponseCurve => budgetResponseCurve;
        public float ProfitWeight => Mathf.Max(0f, profitWeight);
        public float SuccessWeight => Mathf.Max(0f, successWeight);
        public int RecommendedBudget => Mathf.Max(0, recommendedBudget);

        public float EvaluateBudgetMultiplier(int allocatedBudget)
        {
            return budgetResponseCurve != null ? budgetResponseCurve.Evaluate(allocatedBudget) : 1f;
        }
    }
}
