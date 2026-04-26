using System.Collections.Generic;
using CompanySimulator.Features.Employees.Runtime.Components;
using CompanySimulator.Features.Employees.Runtime.Models;
using CompanySimulator.Features.Furniture.Runtime.Components;
using CompanySimulator.Features.Npcs.Runtime.Actors;
using CompanySimulator.Features.Npcs.Runtime.Models;
using UnityEngine;

namespace CompanySimulator.Features.Npcs.Runtime.Office
{
    [DisallowMultipleComponent]
    public sealed class OfficeWorkerManager : MonoBehaviour
    {
        [SerializeField] private EmployeeManager employeeManager;
        [SerializeField] private OfficeDeskCapacityService capacityService;
        [SerializeField] private OfficePointOfInterestService pointOfInterestService;
        [SerializeField] private NpcActor officeWorkerPrefab;
        [SerializeField] private Transform workerActorRoot;
        [SerializeField] private OfficeWorkerBehaviourSettings behaviourSettings = default;

        private readonly Dictionary<EmployeeRuntimeData, OfficeWorkerNpcRuntimeData> runtimeByEmployee = new Dictionary<EmployeeRuntimeData, OfficeWorkerNpcRuntimeData>(32);
        private readonly Dictionary<EmployeeRuntimeData, NpcActor> actorByEmployee = new Dictionary<EmployeeRuntimeData, NpcActor>(32);
        private int workerSequence;

        private void Awake()
        {
            employeeManager ??= FindObjectOfType<EmployeeManager>();
            capacityService ??= FindObjectOfType<OfficeDeskCapacityService>();
            if (capacityService == null)
            {
                capacityService = new GameObject("OfficeDeskCapacityService", typeof(OfficeDeskCapacityService)).GetComponent<OfficeDeskCapacityService>();
            }

            pointOfInterestService ??= FindObjectOfType<OfficePointOfInterestService>();
            if (pointOfInterestService == null)
            {
                pointOfInterestService = new GameObject("OfficePointOfInterestService", typeof(OfficePointOfInterestService)).GetComponent<OfficePointOfInterestService>();
            }

            EnsureActorRoot();
        }

        private void OnEnable()
        {
            employeeManager ??= FindObjectOfType<EmployeeManager>();
            if (employeeManager != null)
            {
                employeeManager.DataChanged -= SyncWorkers;
                employeeManager.DataChanged += SyncWorkers;
            }

            SyncWorkers();
        }

        private void OnDisable()
        {
            if (employeeManager != null)
            {
                employeeManager.DataChanged -= SyncWorkers;
            }
        }

        public int ActiveWorkerCount => runtimeByEmployee.Count;

        private void Update()
        {
            UpdateWorkers();
        }

        public void SyncWorkers()
        {
            if (employeeManager == null || capacityService == null)
            {
                return;
            }

            var desiredEmployees = GetDesiredOfficeEmployees();
            CleanupRemovedEmployees(desiredEmployees);

            var seats = capacityService.GetAllEmployeeSeats();
            var availableSeats = new List<SeatController>(seats.Count);
            for (var i = 0; i < seats.Count; i++)
            {
                var seat = seats[i];
                if (seat == null)
                {
                    continue;
                }

                if (IsSeatUsedByExistingWorker(seat))
                {
                    continue;
                }

                if (!seat.IsOccupied)
                {
                    availableSeats.Add(seat);
                }
            }

            for (var i = 0; i < desiredEmployees.Count; i++)
            {
                var employee = desiredEmployees[i];
                if (employee == null || runtimeByEmployee.ContainsKey(employee))
                {
                    continue;
                }

                if (availableSeats.Count == 0)
                {
                    break;
                }

                var seat = availableSeats[0];
                availableSeats.RemoveAt(0);
                TrySpawnWorker(employee, seat);
            }
        }

        private List<EmployeeRuntimeData> GetDesiredOfficeEmployees()
        {
            var result = new List<EmployeeRuntimeData>(16);
            var employees = employeeManager.Employees;
            for (var i = 0; i < employees.Count; i++)
            {
                var employee = employees[i];
                if (employee?.Role != null && employee.Role.RequiresOffice)
                {
                    result.Add(employee);
                }
            }

            return result;
        }

        private void CleanupRemovedEmployees(List<EmployeeRuntimeData> desiredEmployees)
        {
            var desiredLookup = new HashSet<EmployeeRuntimeData>(desiredEmployees);
            var existingEmployees = new List<EmployeeRuntimeData>(runtimeByEmployee.Keys);
            for (var i = 0; i < existingEmployees.Count; i++)
            {
                var employee = existingEmployees[i];
                if (!desiredLookup.Contains(employee))
                {
                    DespawnWorker(employee);
                }
            }
        }

        private bool IsSeatUsedByExistingWorker(SeatController seat)
        {
            foreach (var pair in runtimeByEmployee)
            {
                if (pair.Value != null && pair.Value.Seat == seat)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TrySpawnWorker(EmployeeRuntimeData employee, SeatController seat)
        {
            if (employee == null || seat == null)
            {
                return false;
            }

            var actor = CreateActorInstance();
            if (actor == null)
            {
                return false;
            }

            if (!seat.TryOccupy(actor, SeatOccupantType.EmployeeNpc))
            {
                Destroy(actor.gameObject);
                return false;
            }

            var desk = seat.GetComponentInParent<OfficeDeskController>();
            var settings = behaviourSettings.MoveSpeed > 0f ? behaviourSettings : OfficeWorkerBehaviourSettings.Default;
            var runtime = new OfficeWorkerNpcRuntimeData($"office_worker_{++workerSequence}", employee, desk, seat, settings);
            runtime.SetPose(seat.GetSeatPosition(), seat.GetSeatRotation());
            runtime.SetLifecycleState(NpcLifecycleState.Seated);
            actor.Bind(runtime);
            actor.SetSeatedPresentation(true, false, false);
            runtimeByEmployee.Add(employee, runtime);
            actorByEmployee.Add(employee, actor);
            return true;
        }

        private void UpdateWorkers()
        {
            foreach (var pair in runtimeByEmployee)
            {
                var employee = pair.Key;
                var runtime = pair.Value;
                if (runtime == null || !actorByEmployee.TryGetValue(employee, out var actor) || actor == null)
                {
                    continue;
                }

                switch (runtime.State)
                {
                    case OfficeWorkerState.Seated:
                        UpdateSeatedWorker(runtime, actor);
                        break;
                    case OfficeWorkerState.WalkingToWanderPoint:
                        UpdateWalkingToWanderPoint(runtime, actor);
                        break;
                    case OfficeWorkerState.Wandering:
                        UpdateWanderingWorker(runtime, actor);
                        break;
                    case OfficeWorkerState.WalkingToSeat:
                        UpdateWalkingToSeat(runtime, actor);
                        break;
                }
            }
        }

        private void UpdateSeatedWorker(OfficeWorkerNpcRuntimeData runtime, NpcActor actor)
        {
            runtime.Tick(UnityEngine.Time.deltaTime);
            runtime.SetPose(runtime.Seat.GetSeatPosition(), runtime.Seat.GetSeatRotation());
            actor.ApplyRuntimePose();
            actor.SetSeatedPresentation(true, false, false);

            if (runtime.RemainingStateTime > 0f)
            {
                return;
            }

            runtime.Seat.Vacate(actor);
            if (TryAssignPointOfInterest(runtime, out var visitDuration))
            {
                runtime.SetState(OfficeWorkerState.WalkingToWanderPoint, visitDuration);
            }
            else
            {
                runtime.SetWanderTarget(ResolveWanderTarget(runtime));
                runtime.SetState(OfficeWorkerState.WalkingToWanderPoint, runtime.BehaviourSettings.MaxWanderDuration);
            }

            runtime.SetLifecycleState(NpcLifecycleState.Walking);
        }

        private void UpdateWalkingToWanderPoint(OfficeWorkerNpcRuntimeData runtime, NpcActor actor)
        {
            var reached = actor.MoveTowards(runtime.WanderTarget, runtime.BehaviourSettings.MoveSpeed);
            runtime.SetPose(actor.transform.position, actor.transform.rotation);
            if (!reached)
            {
                return;
            }

            var visitDuration = runtime.CurrentPointOfInterest != null
                ? runtime.CurrentPointOfInterest.GetRandomVisitDuration()
                : Random.Range(runtime.BehaviourSettings.MinWanderDuration, runtime.BehaviourSettings.MaxWanderDuration);
            runtime.SetState(OfficeWorkerState.Wandering, visitDuration);
            runtime.SetLifecycleState(NpcLifecycleState.Spawned);
            actor.SetMovingPresentation(0f);
        }

        private void UpdateWanderingWorker(OfficeWorkerNpcRuntimeData runtime, NpcActor actor)
        {
            runtime.Tick(UnityEngine.Time.deltaTime);
            actor.SetMovingPresentation(0f);
            if (runtime.RemainingStateTime > 0f)
            {
                return;
            }

            ReleasePointOfInterest(runtime);
            runtime.SetState(OfficeWorkerState.WalkingToSeat, runtime.BehaviourSettings.MaxSeatDuration);
            runtime.SetLifecycleState(NpcLifecycleState.Walking);
        }

        private void UpdateWalkingToSeat(OfficeWorkerNpcRuntimeData runtime, NpcActor actor)
        {
            var seatPosition = runtime.Seat.GetSeatPosition();
            var reached = actor.MoveTowards(seatPosition, runtime.BehaviourSettings.MoveSpeed);
            runtime.SetPose(actor.transform.position, actor.transform.rotation);
            if (!reached)
            {
                return;
            }

            if (!runtime.Seat.TryOccupy(actor, SeatOccupantType.EmployeeNpc))
            {
                ReleasePointOfInterest(runtime);
                runtime.SetWanderTarget(ResolveWanderTarget(runtime));
                runtime.SetState(OfficeWorkerState.WalkingToWanderPoint, runtime.BehaviourSettings.MaxWanderDuration);
                return;
            }

            runtime.SetPose(runtime.Seat.GetSeatPosition(), runtime.Seat.GetSeatRotation());
            runtime.SetLifecycleState(NpcLifecycleState.Seated);
            runtime.SetState(OfficeWorkerState.Seated, Random.Range(runtime.BehaviourSettings.MinSeatDuration, runtime.BehaviourSettings.MaxSeatDuration));
            actor.ApplyRuntimePose();
            actor.SetSeatedPresentation(true, false, false);
        }

        private static Vector3 ResolveWanderTarget(OfficeWorkerNpcRuntimeData runtime)
        {
            var origin = runtime.Seat != null ? runtime.Seat.GetSeatPosition() : runtime.WorldPosition;
            var randomOffset = Random.insideUnitSphere * runtime.BehaviourSettings.WanderRadius;
            randomOffset.y = 0f;
            return origin + randomOffset;
        }

        private bool TryAssignPointOfInterest(OfficeWorkerNpcRuntimeData runtime, out float visitDuration)
        {
            visitDuration = runtime != null ? runtime.BehaviourSettings.MaxWanderDuration : 0f;
            if (runtime == null)
            {
                return false;
            }

            runtime.ClearCurrentPointOfInterest();
            if (pointOfInterestService == null)
            {
                return false;
            }

            if (!pointOfInterestService.TrySelectPoint(runtime, out var pointOfInterest) || pointOfInterest == null)
            {
                return false;
            }

            runtime.SetCurrentPointOfInterest(pointOfInterest);
            runtime.SetWanderTarget(pointOfInterest.VisitPosition);
            visitDuration = pointOfInterest.GetRandomVisitDuration();
            return true;
        }

        private static void ReleasePointOfInterest(OfficeWorkerNpcRuntimeData runtime)
        {
            if (runtime?.CurrentPointOfInterest == null)
            {
                return;
            }

            runtime.CurrentPointOfInterest.Release(runtime.RuntimeId);
            runtime.ClearCurrentPointOfInterest();
        }

        private void DespawnWorker(EmployeeRuntimeData employee)
        {
            if (employee == null)
            {
                return;
            }

            if (runtimeByEmployee.TryGetValue(employee, out var runtime) && runtime?.Seat != null)
            {
                ReleasePointOfInterest(runtime);
                runtime.Seat.Vacate(actorByEmployee.TryGetValue(employee, out var actorRef) ? actorRef : null);
            }

            if (actorByEmployee.TryGetValue(employee, out var actor) && actor != null)
            {
                Destroy(actor.gameObject);
            }

            actorByEmployee.Remove(employee);
            runtimeByEmployee.Remove(employee);
        }

        private NpcActor CreateActorInstance()
        {
            if (officeWorkerPrefab != null)
            {
                return Instantiate(officeWorkerPrefab, workerActorRoot);
            }

            var primitive = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            primitive.name = "OfficeWorkerNpcActor";
            primitive.transform.SetParent(workerActorRoot, false);
            primitive.transform.localScale = new Vector3(0.6f, 1.8f, 0.6f);
            var primitiveCollider = primitive.GetComponent<Collider>();
            if (primitiveCollider != null)
            {
                primitiveCollider.enabled = false;
            }

            return primitive.AddComponent<NpcActor>();
        }

        private void EnsureActorRoot()
        {
            if (workerActorRoot != null)
            {
                return;
            }

            var existing = transform.Find("OfficeWorkerActorRoot");
            if (existing != null)
            {
                workerActorRoot = existing;
                return;
            }

            workerActorRoot = new GameObject("OfficeWorkerActorRoot").transform;
            workerActorRoot.SetParent(transform, false);
        }
    }
}
