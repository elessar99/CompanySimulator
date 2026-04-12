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
            return SeatPoint != null ? SeatPoint.SeatPosition : transform.position;
        }

        public Quaternion GetSeatRotation()
        {
            return SeatPoint != null ? SeatPoint.SeatRotation : transform.rotation;
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
