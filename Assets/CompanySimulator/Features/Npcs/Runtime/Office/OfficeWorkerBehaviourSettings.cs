using UnityEngine;

namespace CompanySimulator.Features.Npcs.Runtime.Office
{
    [System.Serializable]
    public struct OfficeWorkerBehaviourSettings
    {
        [SerializeField, Min(0.5f)] private float minSeatDuration;
        [SerializeField, Min(0.5f)] private float maxSeatDuration;
        [SerializeField, Min(0.5f)] private float minWanderDuration;
        [SerializeField, Min(0.5f)] private float maxWanderDuration;
        [SerializeField, Min(0.5f)] private float wanderRadius;
        [SerializeField, Min(0.1f)] private float moveSpeed;

        public float MinSeatDuration => Mathf.Max(0.5f, minSeatDuration);
        public float MaxSeatDuration => Mathf.Max(MinSeatDuration, maxSeatDuration);
        public float MinWanderDuration => Mathf.Max(0.5f, minWanderDuration);
        public float MaxWanderDuration => Mathf.Max(MinWanderDuration, maxWanderDuration);
        public float WanderRadius => Mathf.Max(0.5f, wanderRadius);
        public float MoveSpeed => Mathf.Max(0.1f, moveSpeed);

        public static OfficeWorkerBehaviourSettings Default => new OfficeWorkerBehaviourSettings
        {
            minSeatDuration = 6f,
            maxSeatDuration = 12f,
            minWanderDuration = 2f,
            maxWanderDuration = 4f,
            wanderRadius = 2.25f,
            moveSpeed = 1.6f
        };
    }
}
