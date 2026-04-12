using UnityEngine;

namespace CompanySimulator.Features.Furniture.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class SeatPoint : MonoBehaviour
    {
        [SerializeField] private SeatOccupantType allowedOccupantType = SeatOccupantType.Any;
        [SerializeField] private Transform seatAnchor;
        [SerializeField] private Transform exitAnchor;

        public SeatOccupantType AllowedOccupantType => allowedOccupantType;
        public Transform SeatAnchor => seatAnchor != null ? seatAnchor : transform;
        public Transform ExitAnchor => exitAnchor;
        public Vector3 SeatPosition => SeatAnchor.position;
        public Quaternion SeatRotation => SeatAnchor.rotation;

        public Vector3 GetExitPosition(Transform fallback)
        {
            if (exitAnchor != null)
            {
                return exitAnchor.position;
            }

            return fallback != null
                ? fallback.position
                : SeatAnchor.position + SeatAnchor.forward * 0.75f;
        }

        public Quaternion GetExitRotation(Transform fallback)
        {
            if (exitAnchor != null)
            {
                return exitAnchor.rotation;
            }

            return fallback != null ? fallback.rotation : SeatAnchor.rotation;
        }
    }
}
