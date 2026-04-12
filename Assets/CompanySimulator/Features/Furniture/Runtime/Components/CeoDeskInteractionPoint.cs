using CompanySimulator.Features.Furniture.Runtime.Interactions;
using CompanySimulator.Features.Player.Runtime.Components;
using UnityEngine;

namespace CompanySimulator.Features.Furniture.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class CeoDeskInteractionPoint : MonoBehaviour, IInteractable
    {
        [SerializeField] private CeoDeskController controller;
        [SerializeField] private CeoDeskInteractionMode interactionMode;

        public bool CanInteract(PlayerInteractor interactor)
        {
            controller ??= GetComponentInParent<CeoDeskController>();
            return controller != null && controller.CanInteract(interactor, interactionMode);
        }

        public string GetInteractionText(PlayerInteractor interactor)
        {
            controller ??= GetComponentInParent<CeoDeskController>();
            return controller != null ? controller.GetInteractionText(interactor, interactionMode) : string.Empty;
        }

        public void Interact(PlayerInteractor interactor)
        {
            controller ??= GetComponentInParent<CeoDeskController>();
            controller?.Interact(interactor, interactionMode);
        }
    }
}
