using CompanySimulator.Shared.Runtime.Definitions;
using UnityEngine;

namespace CompanySimulator.Features.Finance.Runtime.Definitions
{
    [CreateAssetMenu(fileName = "BudgetResponseCurveDefinition", menuName = "Company Simulator/Definitions/Finance/Budget Response Curve")]
    public sealed class BudgetResponseCurveDefinition : DefinitionBase
    {
        [SerializeField, Min(1)] private int referenceBudget = 100000;
        [SerializeField] private AnimationCurve multiplierCurve = new AnimationCurve(
            new Keyframe(0f, 0.1f),
            new Keyframe(1f, 1f),
            new Keyframe(2f, 2f));

        public int ReferenceBudget => Mathf.Max(1, referenceBudget);

        public float Evaluate(int allocatedBudget)
        {
            var budgetRatio = Mathf.Max(0f, allocatedBudget / (float)ReferenceBudget);

            if (multiplierCurve == null || multiplierCurve.length == 0)
            {
                return Mathf.Max(0f, budgetRatio);
            }

            return Mathf.Max(0f, multiplierCurve.Evaluate(budgetRatio));
        }
    }
}
