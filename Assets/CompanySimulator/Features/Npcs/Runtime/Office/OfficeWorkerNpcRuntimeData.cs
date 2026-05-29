using CompanySimulator.Features.Employees.Runtime.Models;
using CompanySimulator.Features.Furniture.Runtime.Components;
using CompanySimulator.Features.Npcs.Runtime.Models;
using UnityEngine;

namespace CompanySimulator.Features.Npcs.Runtime.Office
{
    public sealed class OfficeWorkerNpcRuntimeData : NpcRuntimeData
    {
        public OfficeWorkerNpcRuntimeData(string runtimeId, EmployeeRuntimeData employee, OfficeDeskController desk, SeatController seat, OfficeWorkerBehaviourSettings behaviourSettings)
            : base(runtimeId, NpcKind.OfficeWorker, employee != null ? employee.DisplayName : string.Empty)
        {
            Employee = employee;
            Desk = desk;
            Seat = seat;
            BehaviourSettings = behaviourSettings;
            State = OfficeWorkerState.Seated;
            RemainingStateTime = Random.Range(behaviourSettings.MinSeatDuration, behaviourSettings.MaxSeatDuration);
            WanderTarget = seat != null ? seat.GetSeatPosition(SeatOccupantType.EmployeeNpc) : Vector3.zero;
        }

        public EmployeeRuntimeData Employee { get; }
        public OfficeDeskController Desk { get; }
        public SeatController Seat { get; }
        public OfficeWorkerBehaviourSettings BehaviourSettings { get; }
        public OfficeWorkerState State { get; private set; }
        public float RemainingStateTime { get; private set; }
        public Vector3 WanderTarget { get; private set; }
        public OfficePointOfInterest CurrentPointOfInterest { get; private set; }

        public void SetState(OfficeWorkerState state, float remainingStateTime)
        {
            State = state;
            RemainingStateTime = Mathf.Max(0f, remainingStateTime);
        }

        public void Tick(float deltaTime)
        {
            RemainingStateTime = Mathf.Max(0f, RemainingStateTime - Mathf.Max(0f, deltaTime));
        }

        public void SetWanderTarget(Vector3 target)
        {
            WanderTarget = target;
        }

        public void SetCurrentPointOfInterest(OfficePointOfInterest pointOfInterest)
        {
            CurrentPointOfInterest = pointOfInterest;
        }

        public void ClearCurrentPointOfInterest()
        {
            CurrentPointOfInterest = null;
        }
    }
}
