using UnityEngine;

namespace CompanySimulator.Features.Office.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class OfficePlacementArea : MonoBehaviour
    {
        [SerializeField] private OfficeRoom room;
        [SerializeField] private Collider areaCollider;

        public OfficeRoom Room => ResolveRoom();
        public bool AllowsPlacement => Room != null && Room.IsUnlocked && isActiveAndEnabled;

        private void Awake()
        {
            ResolveRoom();
            areaCollider ??= GetComponent<Collider>();
        }

        private void OnValidate()
        {
            areaCollider ??= GetComponent<Collider>();
            if (room == null)
            {
                room = GetComponentInParent<OfficeRoom>();
            }
        }

        public void AssignRoom(OfficeRoom ownerRoom)
        {
            if (ownerRoom != null)
            {
                room = ownerRoom;
            }
        }

        public bool ContainsPlacementPoint(Vector3 worldPosition, Collider hitCollider)
        {
            if (hitCollider != null && hitCollider.GetComponentInParent<OfficePlacementArea>() == this)
            {
                return true;
            }

            areaCollider ??= GetComponent<Collider>();
            if (areaCollider == null || !areaCollider.enabled || !gameObject.activeInHierarchy)
            {
                return false;
            }

            var closestPoint = areaCollider.ClosestPoint(worldPosition);
            return (closestPoint - worldPosition).sqrMagnitude <= 0.0001f;
        }

        private OfficeRoom ResolveRoom()
        {
            if (room == null)
            {
                room = GetComponentInParent<OfficeRoom>();
            }

            return room;
        }
    }
}
