using UnityEngine;

namespace CompanySimulator.Features.Furniture.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class SeatPoint : MonoBehaviour
    {
        [SerializeField] private SeatOccupantType allowedOccupantType = SeatOccupantType.Any;
        [SerializeField] private Transform seatAnchor;
        [SerializeField] private Transform exitAnchor;
        [SerializeField] private Vector3 anySeatedLocalOffset;
        [SerializeField] private Vector3 anySeatedEulerOffset;
        [SerializeField] private Vector3 playerSeatedLocalOffset;
        [SerializeField] private Vector3 playerSeatedEulerOffset;
        [SerializeField] private Vector3 employeeNpcSeatedLocalOffset;
        [SerializeField] private Vector3 employeeNpcSeatedEulerOffset;
        [SerializeField] private Vector3 interviewNpcSeatedLocalOffset;
        [SerializeField] private Vector3 interviewNpcSeatedEulerOffset;

        public SeatOccupantType AllowedOccupantType => allowedOccupantType;
        public Transform SeatAnchor => seatAnchor != null ? seatAnchor : transform;
        public Transform ExitAnchor => exitAnchor;
        public Vector3 SeatPosition => GetSeatPosition(allowedOccupantType);
        public Quaternion SeatRotation => GetSeatRotation(allowedOccupantType);

        public Vector3 GetSeatPosition(SeatOccupantType occupantType)
        {
            return SeatAnchor.TransformPoint(GetSeatedLocalOffset(occupantType));
        }

        public Quaternion GetSeatRotation(SeatOccupantType occupantType)
        {
            return SeatAnchor.rotation * Quaternion.Euler(GetSeatedEulerOffset(occupantType));
        }

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

        private Vector3 GetSeatedLocalOffset(SeatOccupantType occupantType)
        {
            switch (occupantType)
            {
                case SeatOccupantType.Player:
                    return playerSeatedLocalOffset;
                case SeatOccupantType.EmployeeNpc:
                    return employeeNpcSeatedLocalOffset;
                case SeatOccupantType.InterviewNpc:
                    return interviewNpcSeatedLocalOffset;
                default:
                    return anySeatedLocalOffset;
            }
        }

        private Vector3 GetSeatedEulerOffset(SeatOccupantType occupantType)
        {
            switch (occupantType)
            {
                case SeatOccupantType.Player:
                    return playerSeatedEulerOffset;
                case SeatOccupantType.EmployeeNpc:
                    return employeeNpcSeatedEulerOffset;
                case SeatOccupantType.InterviewNpc:
                    return interviewNpcSeatedEulerOffset;
                default:
                    return anySeatedEulerOffset;
            }
        }
    }
}
