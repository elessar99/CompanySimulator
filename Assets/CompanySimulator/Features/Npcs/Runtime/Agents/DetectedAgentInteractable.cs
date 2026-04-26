using CompanySimulator.Features.Furniture.Runtime.Interactions;
using CompanySimulator.Features.Player.Runtime.Components;
using UnityEngine;

namespace CompanySimulator.Features.Npcs.Runtime.Agents
{
    [DisallowMultipleComponent]
    public sealed class DetectedAgentInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private DetectedAgentManager detectedAgentManager;
        [SerializeField] private string runtimeId;
        [SerializeField, Min(0.5f)] private float dismissDistance = 2f;

        public void Configure(DetectedAgentManager manager, string agentRuntimeId)
        {
            detectedAgentManager = manager;
            runtimeId = agentRuntimeId;
        }

        public bool CanInteract(PlayerInteractor interactor)
        {
            detectedAgentManager ??= FindObjectOfType<DetectedAgentManager>();
            if (detectedAgentManager == null || string.IsNullOrWhiteSpace(runtimeId) || interactor == null)
            {
                return false;
            }

            return true;
        }

        public string GetInteractionText(PlayerInteractor interactor)
        {
            return CanInteract(interactor) ? "Ajanı yakala" : string.Empty;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            detectedAgentManager.TryDismissAgent(runtimeId);
        }
    }
}
