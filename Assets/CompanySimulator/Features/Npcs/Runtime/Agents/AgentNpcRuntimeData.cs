using CompanySimulator.Features.Agents.Runtime.Models;
using CompanySimulator.Features.Npcs.Runtime.Models;
using CompanySimulator.Features.Npcs.Runtime.Office;
using UnityEngine;

namespace CompanySimulator.Features.Npcs.Runtime.Agents
{
    public sealed class AgentNpcRuntimeData : NpcRuntimeData
    {
        public AgentNpcRuntimeData(string runtimeId, PlayerTargetedAgentRuntimeData sourceAgent, AgentNpcBehaviourSettings behaviourSettings)
            : base(runtimeId, NpcKind.Agent, sourceAgent != null && sourceAgent.Definition != null ? sourceAgent.Definition.DisplayName : "Ajan")
        {
            SourceAgent = sourceAgent;
            BehaviourSettings = behaviourSettings;
            State = AgentNpcState.Wandering;
            RemainingStateTime = Random.Range(behaviourSettings.MinVisitDuration, behaviourSettings.MaxVisitDuration);
        }

        public PlayerTargetedAgentRuntimeData SourceAgent { get; }
        public AgentNpcBehaviourSettings BehaviourSettings { get; }
        public AgentNpcState State { get; private set; }
        public float RemainingStateTime { get; private set; }
        public Vector3 CurrentTarget { get; private set; }
        public OfficePointOfInterest CurrentPointOfInterest { get; private set; }

        public void SetState(AgentNpcState state, float remainingStateTime)
        {
            State = state;
            RemainingStateTime = Mathf.Max(0f, remainingStateTime);
        }

        public void Tick(float deltaTime)
        {
            RemainingStateTime = Mathf.Max(0f, RemainingStateTime - Mathf.Max(0f, deltaTime));
        }

        public void SetCurrentTarget(Vector3 target)
        {
            CurrentTarget = target;
        }

        public void SetCurrentPointOfInterest(OfficePointOfInterest pointOfInterest)
        {
            CurrentPointOfInterest = pointOfInterest;
        }

        public void ClearCurrentPointOfInterest()
        {
            CurrentPointOfInterest = null;
        }
    }
}
