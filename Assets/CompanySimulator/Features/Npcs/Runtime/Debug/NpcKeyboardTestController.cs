using CompanySimulator.Features.Npcs.Runtime.Actors;
using CompanySimulator.Features.Npcs.Runtime.Models;
using UnityEngine;
using UnityEngine.AI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CompanySimulator.Features.Npcs.Runtime.Debug
{
    [DisallowMultipleComponent]
    public sealed class NpcKeyboardTestController : MonoBehaviour
    {
        [Header("Spawn")]
        [SerializeField] private NpcActor testNpcPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform actorRoot;
        [SerializeField] private bool replaceExistingTestNpc = true;
        [SerializeField] private bool snapSpawnToNavMesh = true;
        [SerializeField] private bool snapSpawnToGround = true;
        [SerializeField] private LayerMask groundMask = Physics.DefaultRaycastLayers;
        [SerializeField, Min(0f)] private float groundRaycastHeight = 3f;
        [SerializeField, Min(0.1f)] private float groundRaycastDistance = 8f;

        [Header("Seating")]
        [SerializeField] private Transform testSeatAnchor;
        [SerializeField] private bool applyManualSeatOffsetWithoutAnchor;
        [SerializeField] private Vector3 manualSeatPositionOffset;
        [SerializeField] private bool restoreStandingPoseOnStand = true;

        [Header("Movement")]
        [SerializeField, Min(0.1f)] private float moveSpeed = 3f;
        [SerializeField, Min(0f)] private float minMoveDistance = 10f;
        [SerializeField, Min(0f)] private float maxMoveDistance = 20f;
        [SerializeField, Min(0f)] private float navMeshSampleRadius = 4f;

        [Header("Keys")]
        [SerializeField] private KeyCode spawnKey = KeyCode.Alpha0;
        [SerializeField] private KeyCode randomMoveKey = KeyCode.Alpha1;
        [SerializeField] private KeyCode sitKey = KeyCode.Alpha2;
        [SerializeField] private KeyCode standKey = KeyCode.Alpha3;
        [SerializeField] private bool logActions = true;

        private NpcActor currentActor;
        private TestNpcRuntimeData currentRuntime;
        private Vector3 currentMoveTarget;
        private bool isMoving;
        private int spawnSequence;
        private bool hasStoredStandingPose;
        private Vector3 standingPositionBeforeSit;
        private Quaternion standingRotationBeforeSit;

        private void Update()
        {
            if (WasKeyPressed(spawnKey))
            {
                SpawnTestNpc();
            }

            if (WasKeyPressed(randomMoveKey))
            {
                MoveTestNpcToRandomTarget();
            }

            if (WasKeyPressed(sitKey))
            {
                SetTestNpcSeated(true);
            }

            if (WasKeyPressed(standKey))
            {
                SetTestNpcSeated(false);
            }

            UpdateMovement();
        }

        private void SpawnTestNpc()
        {
            EnsureActorRoot();
            var sourceTransform = spawnPoint != null ? spawnPoint : transform;
            var spawnPosition = ResolveSpawnPosition(sourceTransform.position);

            if (replaceExistingTestNpc && currentActor != null)
            {
                SetActorCollidersEnabled(currentActor, false);
                Destroy(currentActor.gameObject);
                currentActor = null;
                currentRuntime = null;
                isMoving = false;
            }

            currentActor = CreateActorInstance();
            if (currentActor == null)
            {
                Log("Test NPC prefab bulunamadi.");
                return;
            }

            currentActor.transform.SetPositionAndRotation(spawnPosition, sourceTransform.rotation);
            currentRuntime = new TestNpcRuntimeData($"test_npc_{++spawnSequence}", "Test NPC");
            currentRuntime.SetPose(spawnPosition, sourceTransform.rotation);
            currentRuntime.SetLifecycleState(NpcLifecycleState.Spawned);
            currentActor.Bind(currentRuntime);
            currentActor.SetSeatedPresentation(false);
            isMoving = false;
            hasStoredStandingPose = false;
            Log("Test NPC spawnlandi. 1: yurume, 2: otur, 3: kalk.");
        }

        private void MoveTestNpcToRandomTarget()
        {
            if (!EnsureTestNpc())
            {
                return;
            }

            RestoreStandingPoseIfStored();
            currentMoveTarget = PickRandomDestination(currentActor.transform.position);
            currentRuntime.SetLifecycleState(NpcLifecycleState.Walking);
            currentActor.SetSeatedPresentation(false);
            isMoving = true;
            Log($"Test NPC hedefe yurutuluyor: {currentMoveTarget}");
        }

        private void SetTestNpcSeated(bool isSeated)
        {
            if (!EnsureTestNpc())
            {
                return;
            }

            isMoving = false;
            if (isSeated)
            {
                StoreStandingPose();
                var seatPosition = testSeatAnchor != null
                    ? testSeatAnchor.position
                    : currentActor.transform.position + (applyManualSeatOffsetWithoutAnchor ? manualSeatPositionOffset : Vector3.zero);
                var seatRotation = testSeatAnchor != null ? testSeatAnchor.rotation : currentActor.transform.rotation;
                currentRuntime.SetPose(seatPosition, seatRotation);
                currentActor.ApplyRuntimePose();
            }
            else
            {
                RestoreStandingPoseIfStored();
            }

            currentRuntime.SetLifecycleState(isSeated ? NpcLifecycleState.Seated : NpcLifecycleState.Spawned);
            currentActor.SetSeatedPresentation(isSeated);
            Log(isSeated ? "Test NPC oturtuldu." : "Test NPC kaldirildi.");
        }

        private void UpdateMovement()
        {
            if (!isMoving || currentActor == null || currentRuntime == null)
            {
                return;
            }

            var reached = currentActor.MoveTowards(currentMoveTarget, moveSpeed);
            currentRuntime.SetPose(currentActor.transform.position, currentActor.transform.rotation);
            currentRuntime.SetLifecycleState(reached ? NpcLifecycleState.Spawned : NpcLifecycleState.Walking);

            if (!reached)
            {
                return;
            }

            isMoving = false;
            currentActor.SetMovingPresentation(0f);
            Log("Test NPC hedefe ulasti.");
        }

        private void StoreStandingPose()
        {
            if (currentActor == null || hasStoredStandingPose)
            {
                return;
            }

            standingPositionBeforeSit = currentActor.transform.position;
            standingRotationBeforeSit = currentActor.transform.rotation;
            hasStoredStandingPose = true;
        }

        private void RestoreStandingPoseIfStored()
        {
            if (!restoreStandingPoseOnStand || !hasStoredStandingPose || currentActor == null || currentRuntime == null)
            {
                return;
            }

            currentRuntime.SetPose(standingPositionBeforeSit, standingRotationBeforeSit);
            currentActor.ApplyRuntimePose();
            hasStoredStandingPose = false;
        }

        private NpcActor CreateActorInstance()
        {
            if (testNpcPrefab != null)
            {
                return Instantiate(testNpcPrefab, actorRoot);
            }

            var primitive = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            primitive.name = "TestNpcActor_Fallback";
            primitive.transform.SetParent(actorRoot, false);
            primitive.transform.localScale = new Vector3(0.6f, 1.8f, 0.6f);

            var primitiveCollider = primitive.GetComponent<Collider>();
            if (primitiveCollider != null)
            {
                primitiveCollider.enabled = false;
            }

            return primitive.AddComponent<NpcActor>();
        }

        private static void SetActorCollidersEnabled(NpcActor actor, bool isEnabled)
        {
            if (actor == null)
            {
                return;
            }

            var colliders = actor.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = isEnabled;
                }
            }
        }

        private Vector3 ResolveSpawnPosition(Vector3 requestedPosition)
        {
            if (snapSpawnToNavMesh && NavMesh.SamplePosition(requestedPosition, out var navMeshHit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                return navMeshHit.position;
            }

            if (!snapSpawnToGround)
            {
                return requestedPosition;
            }

            var rayOrigin = requestedPosition + Vector3.up * groundRaycastHeight;
            if (Physics.Raycast(rayOrigin, Vector3.down, out var hit, groundRaycastHeight + groundRaycastDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                return hit.point;
            }

            return requestedPosition;
        }

        private Vector3 PickRandomDestination(Vector3 origin)
        {
            var minDistance = Mathf.Min(minMoveDistance, maxMoveDistance);
            var maxDistance = Mathf.Max(minMoveDistance, maxMoveDistance);
            var distance = Random.Range(minDistance, maxDistance);
            var direction = Random.insideUnitCircle.normalized;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = Vector2.right;
            }

            var candidate = origin + new Vector3(direction.x, 0f, direction.y) * distance;
            if (NavMesh.SamplePosition(candidate, out var navMeshHit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                return navMeshHit.position;
            }

            candidate.y = origin.y;
            return candidate;
        }

        private bool EnsureTestNpc()
        {
            if (currentActor != null && currentRuntime != null)
            {
                return true;
            }

            SpawnTestNpc();
            return currentActor != null && currentRuntime != null;
        }

        private void EnsureActorRoot()
        {
            if (actorRoot != null)
            {
                return;
            }

            var existing = transform.Find("NpcKeyboardTestActors");
            if (existing != null)
            {
                actorRoot = existing;
                return;
            }

            actorRoot = new GameObject("NpcKeyboardTestActors").transform;
            actorRoot.SetParent(transform, false);
        }

        private void Log(string message)
        {
            if (logActions)
            {
                UnityEngine.Debug.Log(message, this);
            }
        }

        private static bool WasKeyPressed(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                switch (key)
                {
                    case KeyCode.Alpha0: return keyboard.digit0Key.wasPressedThisFrame;
                    case KeyCode.Alpha1: return keyboard.digit1Key.wasPressedThisFrame;
                    case KeyCode.Alpha2: return keyboard.digit2Key.wasPressedThisFrame;
                    case KeyCode.Alpha3: return keyboard.digit3Key.wasPressedThisFrame;
                    case KeyCode.Keypad0: return keyboard.numpad0Key.wasPressedThisFrame;
                    case KeyCode.Keypad1: return keyboard.numpad1Key.wasPressedThisFrame;
                    case KeyCode.Keypad2: return keyboard.numpad2Key.wasPressedThisFrame;
                    case KeyCode.Keypad3: return keyboard.numpad3Key.wasPressedThisFrame;
                }
            }

            return false;
#else
            return Input.GetKeyDown(key);
#endif
        }

        private sealed class TestNpcRuntimeData : NpcRuntimeData
        {
            public TestNpcRuntimeData(string runtimeId, string displayName)
                : base(runtimeId, NpcKind.OfficeWorker, displayName)
            {
            }
        }
    }
}
