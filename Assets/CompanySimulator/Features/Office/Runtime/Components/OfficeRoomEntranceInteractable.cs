using CompanySimulator.Features.Furniture.Runtime.Interactions;
using CompanySimulator.Features.Player.Runtime.Components;
using UnityEngine;

namespace CompanySimulator.Features.Office.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class OfficeRoomEntranceInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private OfficeManager officeManager;
        [SerializeField] private OfficeRoom room;

        private void Awake()
        {
            ResolveReferences();
        }

        public bool CanInteract(PlayerInteractor interactor)
        {
            ResolveReferences();
            return officeManager != null &&
                   room != null &&
                   !room.IsUnlocked &&
                   officeManager.CanPurchaseRoom(room, out _);
        }

        public string GetInteractionText(PlayerInteractor interactor)
        {
            ResolveReferences();
            if (room == null)
            {
                return string.Empty;
            }

            var roomLabel = $"{room.DisplayName} | {room.SizeLabel}";
            if (room.IsUnlocked)
            {
                return $"{roomLabel} | Oda acik";
            }

            var priceLabel = room.Price.Amount.ToString("N0");
            if (officeManager == null)
            {
                return $"{roomLabel} | Fiyat: {priceLabel} | Ofis sistemi yok";
            }

            return officeManager.CanPurchaseRoom(room, out var validationMessage)
                ? $"{roomLabel} | Fiyat: {priceLabel} | E ile satin al"
                : $"{roomLabel} | Fiyat: {priceLabel} | {validationMessage}";
        }

        public void Interact(PlayerInteractor interactor)
        {
            ResolveReferences();
            if (officeManager == null || room == null)
            {
                return;
            }

            if (!officeManager.TryPurchaseRoom(room, out var validationMessage) &&
                !string.IsNullOrWhiteSpace(validationMessage))
            {
                Debug.Log(validationMessage, this);
            }
        }

        private void ResolveReferences()
        {
            room ??= GetComponentInParent<OfficeRoom>();
            officeManager ??= FindObjectOfType<OfficeManager>();
        }
    }
}
