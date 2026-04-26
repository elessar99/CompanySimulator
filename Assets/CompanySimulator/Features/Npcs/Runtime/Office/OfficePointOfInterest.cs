using UnityEngine;

namespace CompanySimulator.Features.Npcs.Runtime.Office
{
    [DisallowMultipleComponent]
    public sealed class OfficePointOfInterest : MonoBehaviour
    {
        [SerializeField] private OfficePointOfInterestType pointType = OfficePointOfInterestType.Generic;
        [SerializeField] private Transform visitAnchor;
        [SerializeField, Min(0f)] private float selectionWeight = 1f;
        [SerializeField, Min(0.5f)] private float minVisitDuration = 3f;
        [SerializeField, Min(0.5f)] private float maxVisitDuration = 6f;
        [SerializeField] private bool singleOccupancy = true;

        private string reservedByRuntimeId;

        public OfficePointOfInterestType PointType => pointType;
        public Transform VisitAnchor => visitAnchor != null ? visitAnchor : transform;
        public Vector3 VisitPosition => VisitAnchor.position;
        public float SelectionWeight => Mathf.Max(0f, selectionWeight);
        public float MinVisitDuration => Mathf.Max(0.5f, minVisitDuration);
        public float MaxVisitDuration => Mathf.Max(MinVisitDuration, maxVisitDuration);
        public bool SingleOccupancy => singleOccupancy;
        public bool IsReserved => !string.IsNullOrWhiteSpace(reservedByRuntimeId);

        public bool CanReserve(string runtimeId)
        {
            if (string.IsNullOrWhiteSpace(runtimeId))
            {
                return false;
            }

            if (!singleOccupancy)
            {
                return true;
            }

            return string.IsNullOrWhiteSpace(reservedByRuntimeId) || string.Equals(reservedByRuntimeId, runtimeId, System.StringComparison.Ordinal);
        }

        public bool TryReserve(string runtimeId)
        {
            if (!CanReserve(runtimeId))
            {
                return false;
            }

            reservedByRuntimeId = runtimeId;
            return true;
        }

        public void Release(string runtimeId)
        {
            if (string.IsNullOrWhiteSpace(runtimeId))
            {
                return;
            }

            if (string.Equals(reservedByRuntimeId, runtimeId, System.StringComparison.Ordinal))
            {
                reservedByRuntimeId = string.Empty;
            }
        }

        public float GetRandomVisitDuration()
        {
            return Random.Range(MinVisitDuration, MaxVisitDuration);
        }
    }
}
