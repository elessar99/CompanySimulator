using CompanySimulator.Features.Npcs.Runtime.Models;
using UnityEngine;

namespace CompanySimulator.Features.Npcs.Runtime.Animation
{
    [DisallowMultipleComponent]
    public sealed class NpcAnimationBridge : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private string moveSpeedParameter = "MoveSpeed";
        [SerializeField] private string isMovingParameter = "IsMoving";
        [SerializeField] private string isSeatedParameter = "IsSeated";
        [SerializeField] private string isTalkingParameter = "IsTalking";
        [SerializeField] private string isInterviewingParameter = "IsInterviewing";
        [SerializeField] private string isAgentParameter = "IsAgent";
        [SerializeField] private string stateIndexParameter = "StateIndex";

        private void Awake()
        {
            animator ??= GetComponentInChildren<Animator>();
        }

        public void ApplyState(NpcRuntimeData runtimeData, float moveSpeed, bool isMoving, bool isSeated, bool isTalking, bool isInterviewing)
        {
            if (animator == null || runtimeData == null)
            {
                return;
            }

            animator.SetFloat(moveSpeedParameter, moveSpeed);
            animator.SetBool(isMovingParameter, isMoving);
            animator.SetBool(isSeatedParameter, isSeated);
            animator.SetBool(isTalkingParameter, isTalking);
            animator.SetBool(isInterviewingParameter, isInterviewing);
            animator.SetBool(isAgentParameter, runtimeData.Kind == NpcKind.Agent);
            animator.SetInteger(stateIndexParameter, (int)runtimeData.LifecycleState);
        }
    }
}
