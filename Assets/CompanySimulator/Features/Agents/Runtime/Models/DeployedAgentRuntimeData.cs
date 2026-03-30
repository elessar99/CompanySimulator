using System.Collections.Generic;
using CompanySimulator.Features.Agents.Runtime.Definitions;
using CompanySimulator.Features.Rivals.Runtime.Models;
using CompanySimulator.Features.Sectors.Runtime.Definitions;
using CompanySimulator.Shared.Runtime.Economy;

namespace CompanySimulator.Features.Agents.Runtime.Models
{
    public sealed class DeployedAgentRuntimeData
    {
        private readonly List<RivalCompanyJobRuntimeData> affectedJobs = new List<RivalCompanyJobRuntimeData>(4);

        public DeployedAgentRuntimeData(
            AgentDefinition definition,
            RivalCompanyRuntimeData targetRival,
            SectorDefinition targetSector,
            Money cost,
            int deployDay)
        {
            Definition = definition;
            TargetRival = targetRival;
            TargetSector = targetSector;
            Cost = cost;
            DeployDay = deployDay;
            RemainingDays = definition.DetectionDurationDays;
            IsActive = true;
            HasFailed = false;
        }

        public AgentDefinition Definition { get; }
        public RivalCompanyRuntimeData TargetRival { get; }
        public SectorDefinition TargetSector { get; }
        public Money Cost { get; }
        public int DeployDay { get; }
        public int RemainingDays { get; private set; }
        public bool IsActive { get; private set; }
        public bool HasFailed { get; private set; }
        public IReadOnlyList<RivalCompanyJobRuntimeData> AffectedJobs => affectedJobs;

        public void ApplySabotage()
        {
            var jobs = TargetRival.ActiveJobs;
            var sabotaged = 0;

            for (var i = 0; i < jobs.Count && sabotaged < Definition.MaxSimultaneousSabotage; i++)
            {
                var job = jobs[i];
                if (job.Sector != TargetSector || job.IsAgentAffected)
                {
                    continue;
                }

                var roll = UnityEngine.Random.value;
                if (roll > Definition.SuccessChance)
                {
                    continue;
                }

                job.SetAgentEffect(Definition.RevenueReductionMultiplier);
                affectedJobs.Add(job);
                sabotaged++;
            }

            if (affectedJobs.Count == 0)
            {
                HasFailed = true;
                IsActive = false;
                RemainingDays = 0;
            }
        }

        public bool AdvanceDay()
        {
            if (!IsActive)
            {
                return false;
            }

            RemainingDays--;
            if (RemainingDays > 0)
            {
                return true;
            }

            Expire();
            return false;
        }

        public void Expire()
        {
            IsActive = false;
            RemainingDays = 0;

            for (var i = 0; i < affectedJobs.Count; i++)
            {
                affectedJobs[i].ClearAgentEffect();
            }

            affectedJobs.Clear();
        }
    }
}
