using CompanySimulator.Features.Rivals.Runtime.Definitions;
using CompanySimulator.Features.Sectors.Runtime.Definitions;
using CompanySimulator.Shared.Runtime.Economy;

namespace CompanySimulator.Features.Rivals.Runtime.Models
{
    public sealed class RivalCompanyJobRuntimeData
    {
        public RivalCompanyJobRuntimeData(RivalCompanyJobDefinition definition, int startDay)
        {
            Definition = definition;
            StartDay = startDay;
            DaysSinceLastPayout = 0;
            LastEarnedIncome = Money.Zero;
            IsAgentAffected = false;
            AgentRevenueReductionMultiplier = 1f;
        }

        public RivalCompanyJobDefinition Definition { get; }
        public SectorDefinition Sector => Definition.Sector;
        public int StartDay { get; }
        public int DaysSinceLastPayout { get; private set; }
        public Money LastEarnedIncome { get; private set; }
        public bool IsAgentAffected { get; private set; }
        public float AgentRevenueReductionMultiplier { get; private set; }

        public Money AdvanceDay(float competitionMultiplier)
        {
            DaysSinceLastPayout++;

            if (DaysSinceLastPayout < Definition.PayoutIntervalDays)
            {
                return Money.Zero;
            }

            DaysSinceLastPayout = 0;
            var income = UnityEngine.Random.Range(Definition.MinimumIncomePerCycle, Definition.MaximumIncomePerCycle + 1);
            var agentMultiplier = IsAgentAffected ? AgentRevenueReductionMultiplier : 1f;
            var earned = Money.From(income * competitionMultiplier * agentMultiplier);
            LastEarnedIncome = earned;
            return earned;
        }

        public void SetAgentEffect(float revenueReductionMultiplier)
        {
            IsAgentAffected = true;
            AgentRevenueReductionMultiplier = revenueReductionMultiplier;
        }

        public void ClearAgentEffect()
        {
            IsAgentAffected = false;
            AgentRevenueReductionMultiplier = 1f;
        }
    }
}
