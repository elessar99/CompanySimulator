using CompanySimulator.Features.Npcs.Runtime.Animation;
using CompanySimulator.Features.Npcs.Runtime.Models;
using UnityEngine;
using UnityEngine.AI;

namespace CompanySimulator.Features.Npcs.Runtime.Actors
{
    [DisallowMultipleComponent]
    public sealed class NpcActor : MonoBehaviour
    {
        [SerializeField] private NpcAnimationBridge animationBridge;
        [SerializeField] private NavMeshAgent navMeshAgent;
        [SerializeField] private Transform visualRoot;

        private NpcRuntimeData runtimeData;
        private Vector3 lastMoveTarget;
        private bool hasMoveTarget;

        public NpcRuntimeData RuntimeData => runtimeData;

        private void Awake()
        {
            animationBridge ??= GetComponentInChildren<NpcAnimationBridge>();
            navMeshAgent ??= GetComponent<NavMeshAgent>();
            visualRoot ??= transform;

            if (navMeshAgent != null)
            {
                navMeshAgent.updateRotation = false;
            }
        }

        public void Bind(NpcRuntimeData npcRuntimeData)
        {
            runtimeData = npcRuntimeData;
            ApplyRuntimePose();
        }

        public void ApplyRuntimePose()
        {
            if (runtimeData == null)
            {
                return;
            }

            if (CanUseNavMesh())
            {
                navMeshAgent.Warp(runtimeData.WorldPosition);
                navMeshAgent.ResetPath();
                hasMoveTarget = false;
            }

            transform.SetPositionAndRotation(runtimeData.WorldPosition, runtimeData.WorldRotation);
        }

        public void SetSeatedPresentation(bool isSeated, bool isTalking = false, bool isInterviewing = false)
        {
            if (runtimeData == null)
            {
                return;
            }

            StopMovement();

            animationBridge?.ApplyState(runtimeData, 0f, false, isSeated, isTalking, isInterviewing);
        }

        public void SetMovingPresentation(float moveSpeed, bool isTalking = false)
        {
            if (runtimeData == null)
            {
                return;
            }

            animationBridge?.ApplyState(runtimeData, moveSpeed, moveSpeed > 0.01f, false, isTalking, false);
        }

        public bool MoveTowards(Vector3 worldTarget, float moveSpeed)
        {
            if (runtimeData == null)
            {
                return true;
            }

            if (CanUseNavMesh())
            {
                navMeshAgent.speed = Mathf.Max(0.01f, moveSpeed);
                navMeshAgent.isStopped = false;

                if (!hasMoveTarget || Vector3.Distance(lastMoveTarget, worldTarget) > 0.1f)
                {
                    hasMoveTarget = navMeshAgent.SetDestination(worldTarget);
                    lastMoveTarget = worldTarget;
                }

                if (!navMeshAgent.pathPending && navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid)
                {
                    StopMovement();
                    return FallbackMoveTowards(worldTarget, moveSpeed);
                }

                if (navMeshAgent.velocity.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(navMeshAgent.velocity.normalized, Vector3.up);
                }

                SetMovingPresentation(moveSpeed);
                if (navMeshAgent.pathPending)
                {
                    return false;
                }

                if (navMeshAgent.remainingDistance <= Mathf.Max(navMeshAgent.stoppingDistance, 0.08f))
                {
                    StopMovement();
                    return true;
                }

                return false;
            }

            return FallbackMoveTowards(worldTarget, moveSpeed);
        }

        private bool FallbackMoveTowards(Vector3 worldTarget, float moveSpeed)
        {
            StopMovement();

            var currentPosition = transform.position;
            var toTarget = worldTarget - currentPosition;
            var planarToTarget = Vector3.ProjectOnPlane(toTarget, Vector3.up);
            var distance = planarToTarget.magnitude;
            if (distance <= 0.05f)
            {
                transform.position = new Vector3(worldTarget.x, transform.position.y, worldTarget.z);
                SetMovingPresentation(0f);
                return true;
            }

            var direction = planarToTarget / Mathf.Max(0.001f, distance);
            transform.position += direction * Mathf.Max(0.01f, moveSpeed) * UnityEngine.Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            SetMovingPresentation(moveSpeed);
            return false;
        }

        private void StopMovement()
        {
            hasMoveTarget = false;
            if (CanUseNavMesh())
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.ResetPath();
            }
        }

        private bool CanUseNavMesh()
        {
            return navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh;
        }

        public void SetVisible(bool isVisible)
        {
            if (visualRoot != null)
            {
                visualRoot.gameObject.SetActive(isVisible);
            }
            else
            {
                gameObject.SetActive(isVisible);
            }
        }
    }
}
