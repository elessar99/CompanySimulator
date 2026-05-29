using UnityEngine;

namespace CompanySimulator.Features.Furniture.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class SeatController : MonoBehaviour
    {
        [SerializeField] private SeatPoint seatPoint;

        private Object currentOccupant;
        private SeatOccupantType currentOccupantType;

        public SeatPoint SeatPoint => seatPoint != null ? seatPoint : (seatPoint = GetComponent<SeatPoint>());
        public bool IsOccupied => currentOccupant != null;
        public Object CurrentOccupant => currentOccupant;
        public SeatOccupantType CurrentOccupantType => currentOccupantType;

        public bool IsOccupiedBy(Object occupant)
        {
            return occupant != null && currentOccupant == occupant;
        }

        public bool CanOccupy(Object occupant, SeatOccupantType occupantType)
        {
            if (occupant == null || SeatPoint == null)
            {
                return false;
            }

            if (currentOccupant == null)
            {
                return SeatPoint.AllowedOccupantType == SeatOccupantType.Any || SeatPoint.AllowedOccupantType == occupantType;
            }

            return currentOccupant == occupant;
        }

        public bool TryOccupy(Object occupant, SeatOccupantType occupantType)
        {
            if (!CanOccupy(occupant, occupantType))
            {
                return false;
            }

            currentOccupant = occupant;
            currentOccupantType = occupantType;
            return true;
        }

        public void Vacate(Object occupant)
        {
            if (occupant != null && currentOccupant != occupant)
            {
                return;
            }

            currentOccupant = null;
            currentOccupantType = SeatOccupantType.Any;
        }

        public Vector3 GetSeatPosition()
        {
            return GetSeatPosition(SeatPoint != null ? SeatPoint.AllowedOccupantType : SeatOccupantType.Any);
        }

        public Vector3 GetSeatPosition(SeatOccupantType occupantType)
        {
            return SeatPoint != null ? SeatPoint.GetSeatPosition(occupantType) : transform.position;
        }

        public Quaternion GetSeatRotation()
        {
            return GetSeatRotation(SeatPoint != null ? SeatPoint.AllowedOccupantType : SeatOccupantType.Any);
        }

        public Quaternion GetSeatRotation(SeatOccupantType occupantType)
        {
            return SeatPoint != null ? SeatPoint.GetSeatRotation(occupantType) : transform.rotation;
        }

        public Vector3 GetExitPosition(Transform fallback)
        {
            return SeatPoint != null ? SeatPoint.GetExitPosition(fallback) : (fallback != null ? fallback.position : transform.position);
        }

        public Quaternion GetExitRotation(Transform fallback)
        {
            return SeatPoint != null ? SeatPoint.GetExitRotation(fallback) : (fallback != null ? fallback.rotation : transform.rotation);
        }
    }
}
