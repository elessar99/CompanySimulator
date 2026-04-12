using CompanySimulator.Features.Player.Runtime.Components;

namespace CompanySimulator.Features.Furniture.Runtime.Interactions
{
    public interface IInteractable
    {
        bool CanInteract(PlayerInteractor interactor);
        string GetInteractionText(PlayerInteractor interactor);
        void Interact(PlayerInteractor interactor);
    }
}
