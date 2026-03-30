using CompanySimulator.Shared.Runtime.Definitions;
using UnityEngine;

namespace CompanySimulator.Features.Agents.Runtime.Definitions
{
    [CreateAssetMenu(fileName = "AgentDefinition", menuName = "Company Simulator/Definitions/Agents/Agent")]
    public sealed class AgentDefinition : DefinitionBase
    {
        [SerializeField, Min(1)] private int detectionDurationDays = 14;
        [SerializeField, Min(1)] private int maxSimultaneousSabotage = 2;
        [SerializeField, Range(0f, 1f)] private float successChance = 0.6f;
        [SerializeField, Min(0)] private long minimumCost = 5000;
        [SerializeField, Min(0)] private long maximumCost = 15000;
        [SerializeField, Range(0.01f, 1f)] private float revenueReductionMultiplier = 0.5f;
        [SerializeField, Min(1)] private int selectionWeight = 1;

        public int DetectionDurationDays => Mathf.Max(1, detectionDurationDays);
        public int MaxSimultaneousSabotage => Mathf.Max(1, maxSimultaneousSabotage);
        public float SuccessChance => Mathf.Clamp01(successChance);
        public long MinimumCost => minimumCost;
        public long MaximumCost => maximumCost >= minimumCost ? maximumCost : minimumCost;
        public float RevenueReductionMultiplier => Mathf.Clamp(revenueReductionMultiplier, 0.01f, 1f);
        public int SelectionWeight => Mathf.Max(1, selectionWeight);
    }
}
