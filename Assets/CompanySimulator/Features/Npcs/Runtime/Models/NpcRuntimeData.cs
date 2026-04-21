using UnityEngine;

namespace CompanySimulator.Features.Npcs.Runtime.Models
{
    public abstract class NpcRuntimeData
    {
        protected NpcRuntimeData(string runtimeId, NpcKind kind, string displayName)
        {
            RuntimeId = runtimeId;
            Kind = kind;
            DisplayName = displayName ?? string.Empty;
            LifecycleState = NpcLifecycleState.PendingSpawn;
            WorldRotation = Quaternion.identity;
        }

        public string RuntimeId { get; }
        public NpcKind Kind { get; }
        public string DisplayName { get; }
        public NpcLifecycleState LifecycleState { get; private set; }
        public Vector3 WorldPosition { get; private set; }
        public Quaternion WorldRotation { get; private set; }

        public void SetLifecycleState(NpcLifecycleState lifecycleState)
        {
            LifecycleState = lifecycleState;
        }

        public void SetPose(Vector3 worldPosition, Quaternion worldRotation)
        {
            WorldPosition = worldPosition;
            WorldRotation = worldRotation;
        }
    }
}
