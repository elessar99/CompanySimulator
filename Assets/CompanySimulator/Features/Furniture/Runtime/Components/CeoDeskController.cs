using CompanySimulator.Presentation.UI.Runtime.Common;
using CompanySimulator.Features.Player.Runtime.Components;
using UnityEngine;

namespace CompanySimulator.Features.Furniture.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class CeoDeskController : MonoBehaviour
    {
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private FurnitureInstance furnitureInstance;
        [SerializeField] private SeatController playerSeat;
        [SerializeField] private SeatController interviewSeat;

        public FurnitureInstance FurnitureInstance => furnitureInstance != null ? furnitureInstance : GetComponent<FurnitureInstance>();
        public SeatController PlayerSeat => playerSeat;
        public SeatController InterviewSeat => interviewSeat;

        public bool CanInteract(PlayerInteractor interactor, CeoDeskInteractionMode interactionMode)
        {
            if (interactor == null)
            {
                return false;
            }

            switch (interactionMode)
            {
                case CeoDeskInteractionMode.Seat:
                    return playerSeat != null;
                case CeoDeskInteractionMode.Computer:
                    return playerSeat != null;
                default:
                    return false;
            }
        }

        public string GetInteractionText(PlayerInteractor interactor, CeoDeskInteractionMode interactionMode)
        {
            switch (interactionMode)
            {
                case CeoDeskInteractionMode.Seat:
                    return interactor != null && interactor.IsSeatedAt(playerSeat) ? "Masada oturuyorsun" : "Masaya otur";
                case CeoDeskInteractionMode.Computer:
                    return interactor != null && interactor.IsSeatedAt(playerSeat) ? "Bilgisayarı kullan" : "Masaya otur ve bilgisayarı aç";
                default:
                    return string.Empty;
            }
        }

        public void Interact(PlayerInteractor interactor, CeoDeskInteractionMode interactionMode)
        {
            if (!CanInteract(interactor, interactionMode))
            {
                return;
            }

            switch (interactionMode)
            {
                case CeoDeskInteractionMode.Seat:
                    interactor.TrySit(playerSeat);
                    break;
                case CeoDeskInteractionMode.Computer:
                    if (!interactor.IsSeatedAt(playerSeat) && !interactor.TrySit(playerSeat))
                    {
                        return;
                    }

                    rootCanvas ??= FindObjectOfType<Canvas>();
                    RuntimePanelUiUtility.SetComputerPanelActive(rootCanvas, true);
                    break;
            }
        }
    }
}
