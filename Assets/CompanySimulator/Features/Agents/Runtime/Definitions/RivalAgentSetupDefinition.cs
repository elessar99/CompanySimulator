using System;
using CompanySimulator.Shared.Runtime.Definitions;
using UnityEngine;

namespace CompanySimulator.Features.Agents.Runtime.Definitions
{
    [CreateAssetMenu(fileName = "RivalAgentSetupDefinition", menuName = "Company Simulator/Definitions/Agents/Rival Agent Setup")]
    public sealed class RivalAgentSetupDefinition : DefinitionBase
    {
        [SerializeField] private AgentDefinition[] availableAgents = Array.Empty<AgentDefinition>();

        public AgentDefinition[] AvailableAgents => availableAgents;
    }
}
