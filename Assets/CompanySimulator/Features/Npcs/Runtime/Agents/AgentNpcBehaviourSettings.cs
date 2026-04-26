using UnityEngine;

namespace CompanySimulator.Features.Npcs.Runtime.Agents
{
    [System.Serializable]
    public struct AgentNpcBehaviourSettings
    {
        [SerializeField, Min(0.5f)] private float minVisitDuration;
        [SerializeField, Min(0.5f)] private float maxVisitDuration;
        [SerializeField, Min(0.5f)] private float wanderRadius;
        [SerializeField, Min(0.1f)] private float moveSpeed;

        public float MinVisitDuration => Mathf.Max(0.5f, minVisitDuration);
        public float MaxVisitDuration => Mathf.Max(MinVisitDuration, maxVisitDuration);
        public float WanderRadius => Mathf.Max(0.5f, wanderRadius);
        public float MoveSpeed => Mathf.Max(0.1f, moveSpeed);

        public static AgentNpcBehaviourSettings Default => new AgentNpcBehaviourSettings
        {
            minVisitDuration = 3f,
            maxVisitDuration = 6f,
            wanderRadius = 3.5f,
            moveSpeed = 1.75f
        };
    }
}
