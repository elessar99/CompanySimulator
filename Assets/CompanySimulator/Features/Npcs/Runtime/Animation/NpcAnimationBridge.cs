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
        [SerializeField] private string fallbackIsSeatedParameter = "IsSteated";
        [SerializeField] private bool disableRootMotion = true;

        private void Awake()
        {
            animator ??= GetComponentInChildren<Animator>();
            if (animator != null && disableRootMotion)
            {
                animator.applyRootMotion = false;
            }
        }

        public void ApplyState(NpcRuntimeData runtimeData, float moveSpeed, bool isMoving, bool isSeated, bool isTalking, bool isInterviewing)
        {
            if (animator == null || runtimeData == null)
            {
                return;
            }

            SetFloatIfExists(moveSpeedParameter, moveSpeed);
            SetBoolIfExists(isMovingParameter, isMoving);

            if (!SetBoolIfExists(isSeatedParameter, isSeated))
            {
                SetBoolIfExists(fallbackIsSeatedParameter, isSeated);
            }

            SetBoolIfExists(isTalkingParameter, isTalking);
            SetBoolIfExists(isInterviewingParameter, isInterviewing);
            SetBoolIfExists(isAgentParameter, runtimeData.Kind == NpcKind.Agent);
            SetIntegerIfExists(stateIndexParameter, (int)runtimeData.LifecycleState);
        }

        private bool SetFloatIfExists(string parameterName, float value)
        {
            if (!HasParameter(parameterName, AnimatorControllerParameterType.Float))
            {
                return false;
            }

            animator.SetFloat(parameterName, value);
            return true;
        }

        private bool SetBoolIfExists(string parameterName, bool value)
        {
            if (!HasParameter(parameterName, AnimatorControllerParameterType.Bool))
            {
                return false;
            }

            animator.SetBool(parameterName, value);
            return true;
        }

        private bool SetIntegerIfExists(string parameterName, int value)
        {
            if (!HasParameter(parameterName, AnimatorControllerParameterType.Int))
            {
                return false;
            }

            animator.SetInteger(parameterName, value);
            return true;
        }

        private bool HasParameter(string parameterName, AnimatorControllerParameterType parameterType)
        {
            if (animator == null || string.IsNullOrWhiteSpace(parameterName))
            {
                return false;
            }

            var parameters = animator.parameters;
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (parameter.type == parameterType && string.Equals(parameter.name, parameterName, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
