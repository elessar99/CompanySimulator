using CompanySimulator.Features.Sectors.Runtime.Definitions;
using CompanySimulator.Shared.Runtime.Definitions;
using UnityEngine;

namespace CompanySimulator.Features.Rivals.Runtime.Definitions
{
    [CreateAssetMenu(fileName = "RivalCompanyJobDefinition", menuName = "Company Simulator/Definitions/Rivals/Job")]
    public sealed class RivalCompanyJobDefinition : DefinitionBase
    {
        [SerializeField] private SectorDefinition sector;
        [SerializeField, Min(0)] private long jobCost = 1000;
        [SerializeField, Min(1)] private int payoutIntervalDays = 7;
        [SerializeField, Min(0)] private long minimumIncomePerCycle = 500;
        [SerializeField, Min(0)] private long maximumIncomePerCycle = 2000;
        [SerializeField, Min(1)] private int selectionWeight = 1;
        [SerializeField, Range(0f, 1f)] private float abandonChance = 0.1f;
        [SerializeField, Min(0.1f)] private float abandonRevenueMultiplier = 3f;

        public SectorDefinition Sector => sector;
        public long JobCost => jobCost;
        public int PayoutIntervalDays => Mathf.Max(1, payoutIntervalDays);
        public long MinimumIncomePerCycle => minimumIncomePerCycle;
        public long MaximumIncomePerCycle => maximumIncomePerCycle >= minimumIncomePerCycle ? maximumIncomePerCycle : minimumIncomePerCycle;
        public int SelectionWeight => Mathf.Max(1, selectionWeight);
        public float AbandonChance => Mathf.Clamp01(abandonChance);
        public float AbandonRevenueMultiplier => Mathf.Max(0.1f, abandonRevenueMultiplier);
    }
}
