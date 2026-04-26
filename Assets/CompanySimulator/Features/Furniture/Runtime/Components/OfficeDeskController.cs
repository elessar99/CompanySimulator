using CompanySimulator.Features.Furniture.Runtime.Definitions;
using System.Collections.Generic;
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

        private readonly List<SeatController> employeeSeats = new List<SeatController>(6);

        public FurnitureInstance FurnitureInstance => furnitureInstance != null ? furnitureInstance : GetComponent<FurnitureInstance>();
        public SeatController EmployeeSeat => employeeSeat;
        public Transform ComputerMountPoint => computerMountPoint;
        public bool HasMountedComputer => hasMountedComputer;
        public bool SupportsComputerAttachment => FurnitureInstance != null && FurnitureInstance.Definition != null && FurnitureInstance.Definition.AllowsComputerAttachment;

        private void Awake()
        {
            ResolveEmployeeSeats();
        }

        public void SetMountedComputer(bool isMounted)
        {
            hasMountedComputer = isMounted;
        }

        public IReadOnlyList<SeatController> GetEmployeeSeats()
        {
            ResolveEmployeeSeats();
            return employeeSeats;
        }

        private void ResolveEmployeeSeats()
        {
            employeeSeats.Clear();

            if (employeeSeat != null)
            {
                employeeSeats.Add(employeeSeat);
            }

            var seats = GetComponentsInChildren<SeatController>(true);
            for (var i = 0; i < seats.Length; i++)
            {
                var seat = seats[i];
                if (seat == null || employeeSeats.Contains(seat) || seat.SeatPoint == null)
                {
                    continue;
                }

                if (seat.SeatPoint.AllowedOccupantType == SeatOccupantType.EmployeeNpc)
                {
                    employeeSeats.Add(seat);
                    employeeSeat ??= seat;
                }
            }
        }
    }
}
