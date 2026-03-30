using System.Collections.Generic;
using CompanySimulator.Features.Agents.Runtime.Definitions;
using CompanySimulator.Features.Finance.Runtime.Models;
using CompanySimulator.Features.Rivals.Runtime.Models;
using CompanySimulator.Features.Sectors.Runtime.Definitions;
using CompanySimulator.Shared.Runtime.Economy;

namespace CompanySimulator.Features.Agents.Runtime.Models
{
    public sealed class PlayerTargetedAgentRuntimeData
    {
        private readonly List<ActiveProjectRuntimeEntry> affectedProjects = new List<ActiveProjectRuntimeEntry>(4);

        public PlayerTargetedAgentRuntimeData(
            AgentDefinition definition,
            RivalCompanyRuntimeData sourceRival,
            SectorDefinition targetSector,
            Money cost,
            int deployDay)
        {
            Definition = definition;
            SourceRival = sourceRival;
            TargetSector = targetSector;
            Cost = cost;
            DeployDay = deployDay;
            RemainingDays = definition.DetectionDurationDays;
            IsActive = true;
            HasFailed = false;
        }

        public AgentDefinition Definition { get; }
        public RivalCompanyRuntimeData SourceRival { get; }
        public SectorDefinition TargetSector { get; }
        public Money Cost { get; }
        public int DeployDay { get; }
        public int RemainingDays { get; private set; }
        public bool IsActive { get; private set; }
        public bool HasFailed { get; private set; }
        public IReadOnlyList<ActiveProjectRuntimeEntry> AffectedProjects => affectedProjects;

        public void ApplySabotage(IReadOnlyList<ActiveProjectRuntimeEntry> playerProjects)
        {
            var sabotaged = 0;

            for (var i = 0; i < playerProjects.Count && sabotaged < Definition.MaxSimultaneousSabotage; i++)
            {
                var project = playerProjects[i];
                if (project.Sector != TargetSector || project.IsAgentAffected)
                {
                    continue;
                }

                var roll = UnityEngine.Random.value;
                if (roll > Definition.SuccessChance)
                {
                    continue;
                }

                project.SetAgentEffect(Definition.RevenueReductionMultiplier);
                affectedProjects.Add(project);
                sabotaged++;
            }

            if (affectedProjects.Count == 0)
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

            for (var i = 0; i < affectedProjects.Count; i++)
            {
                affectedProjects[i].ClearAgentEffect();
            }

            affectedProjects.Clear();
        }
    }
}
