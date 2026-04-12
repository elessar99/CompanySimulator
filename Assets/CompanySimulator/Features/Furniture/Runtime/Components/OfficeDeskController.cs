using CompanySimulator.Features.Furniture.Runtime.Definitions;
using UnityEngine;

namespace CompanySimulator.Features.Furniture.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class OfficeDeskController : MonoBehaviour
    {
        [SerializeField] private FurnitureInstance furnitureInstance;
        [SerializeField] private SeatController employeeSeat;
        [SerializeField] private Transform computerMountPoint;
        [SerializeField] private bool hasMountedComputer;

        public FurnitureInstance FurnitureInstance => furnitureInstance != null ? furnitureInstance : GetComponent<FurnitureInstance>();
        public SeatController EmployeeSeat => employeeSeat;
        public Transform ComputerMountPoint => computerMountPoint;
        public bool HasMountedComputer => hasMountedComputer;
        public bool SupportsComputerAttachment => FurnitureInstance != null && FurnitureInstance.Definition != null && FurnitureInstance.Definition.AllowsComputerAttachment;

        public void SetMountedComputer(bool isMounted)
        {
            hasMountedComputer = isMounted;
        }
    }
}
