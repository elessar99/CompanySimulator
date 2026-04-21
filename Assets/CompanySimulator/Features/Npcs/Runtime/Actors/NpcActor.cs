using CompanySimulator.Features.Npcs.Runtime.Animation;
using CompanySimulator.Features.Npcs.Runtime.Models;
using UnityEngine;

namespace CompanySimulator.Features.Npcs.Runtime.Actors
{
    [DisallowMultipleComponent]
    public sealed class NpcActor : MonoBehaviour
    {
        [SerializeField] private NpcAnimationBridge animationBridge;
        [SerializeField] private Transform visualRoot;

        private NpcRuntimeData runtimeData;

        public NpcRuntimeData RuntimeData => runtimeData;

        private void Awake()
        {
            animationBridge ??= GetComponentInChildren<NpcAnimationBridge>();
            visualRoot ??= transform;
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

            transform.SetPositionAndRotation(runtimeData.WorldPosition, runtimeData.WorldRotation);
        }

        public void SetSeatedPresentation(bool isSeated, bool isTalking = false, bool isInterviewing = false)
        {
            if (runtimeData == null)
            {
                return;
            }

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
