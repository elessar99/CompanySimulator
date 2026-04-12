using System;

namespace CompanySimulator.Features.Furniture.Runtime.Definitions
{
    [Flags]
    public enum FurnitureInteractionType
    {
        None = 0,
        PlayerSeat = 1 << 0,
        EmployeeSeat = 1 << 1,
        InterviewSeat = 1 << 2,
        ComputerAccess = 1 << 3,
        Workstation = 1 << 4,
        Decorative = 1 << 5
    }
}
