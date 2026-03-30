using System;
using System.Collections.Generic;
using CompanySimulator.Shared.Runtime.Definitions;
using UnityEngine;

namespace CompanySimulator.Features.Agents.Runtime.Definitions
{
    [CreateAssetMenu(fileName = "AgentSetupDefinition", menuName = "Company Simulator/Definitions/Agents/Setup")]
    public sealed class AgentSetupDefinition : DefinitionBase
    {
        [SerializeField] private AgentDefinition[] availableAgents = Array.Empty<AgentDefinition>();
        [SerializeField, Min(1)] private int refreshIntervalDays = 7;
        [SerializeField, Min(1)] private int minAgentsPerRefresh = 2;
        [SerializeField, Min(1)] private int maxAgentsPerRefresh = 5;

        public IReadOnlyList<AgentDefinition> AvailableAgents => availableAgents;
        public int RefreshIntervalDays => Mathf.Max(1, refreshIntervalDays);
        public int MinAgentsPerRefresh => Mathf.Max(1, minAgentsPerRefresh);
        public int MaxAgentsPerRefresh => Mathf.Max(MinAgentsPerRefresh, maxAgentsPerRefresh);
    }
}
