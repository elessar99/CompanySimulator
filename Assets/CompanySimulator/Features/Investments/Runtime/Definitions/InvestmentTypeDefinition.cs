using CompanySimulator.Features.Finance.Runtime.Definitions;
using CompanySimulator.Shared.Runtime.Definitions;
using UnityEngine;

namespace CompanySimulator.Features.Investments.Runtime.Definitions
{
    public enum InvestmentExpenseMode
    {
        // Yatırım tutarı iş başlatılırken doğrudan kasadan düşer.
        Pesin = 0,

        // Yatırım tutarı işin gelirinden gider gibi düşülür.
        GelirdenDus = 1
    }

    [CreateAssetMenu(fileName = "InvestmentTypeDefinition", menuName = "Company Simulator/Definitions/Investments/Investment Type")]
    public sealed class InvestmentTypeDefinition : DefinitionBase
    {
        [SerializeField] private BudgetResponseCurveDefinition budgetResponseCurve;
        [SerializeField, Min(0f)] private float profitWeight = 1f;
        [SerializeField, Min(0f)] private float successWeight = 1f;
        [SerializeField] private InvestmentExpenseMode expenseMode = InvestmentExpenseMode.Pesin;
        [SerializeField, Min(0)] private int minimumBudget = 25000;
        [SerializeField, Min(0)] private int recommendedBudget = 100000;

        public BudgetResponseCurveDefinition BudgetResponseCurve => budgetResponseCurve;
        public float ProfitWeight => Mathf.Max(0f, profitWeight);
        public float SuccessWeight => Mathf.Max(0f, successWeight);
        public InvestmentExpenseMode ExpenseMode => expenseMode;
        public int MinimumBudget => Mathf.Max(0, minimumBudget);
        public int RecommendedBudget => Mathf.Max(0, recommendedBudget);
        public bool IsRecurringExpense => expenseMode == InvestmentExpenseMode.GelirdenDus;

        public float EvaluateBudgetMultiplier(int allocatedBudget)
        {
            return budgetResponseCurve != null ? budgetResponseCurve.Evaluate(allocatedBudget) : 1f;
        }

        public string GetBudgetEvaluationLabel(int allocatedBudget)
        {
            if (allocatedBudget <= 0)
            {
                return "Yok";
            }

            var safeRecommendedBudget = Mathf.Max(1, RecommendedBudget);
            var ratio = allocatedBudget / (float)safeRecommendedBudget;
            if (ratio < 0.75f)
            {
                return "Düşük";
            }

            if (ratio < 1.5f)
            {
                return "Orta";
            }

            return "Yüksek";
        }
    }
}
