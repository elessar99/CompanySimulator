using CompanySimulator.Features.Furniture.Runtime.Components;
using CompanySimulator.Features.Furniture.Runtime.Interactions;
using CompanySimulator.Presentation.UI.Runtime.Common;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CompanySimulator.Features.Player.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Camera interactionCamera;
        [SerializeField] private PlayerMovementController movementController;
        [SerializeField] private Canvas rootCanvas;
        [SerializeField, Min(0.5f)] private float interactionDistance = 3f;
        [SerializeField] private LayerMask interactionMask = Physics.DefaultRaycastLayers;
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private KeyCode standUpKey = KeyCode.Q;
        [SerializeField] private bool includeTriggerColliders = true;

        private SeatController currentSeat;

        public bool IsSeated => currentSeat != null;
        public SeatController CurrentSeat => currentSeat;

        private void Awake()
        {
            movementController ??= GetComponent<PlayerMovementController>();
            interactionCamera ??= movementController != null ? movementController.PlayerCamera : GetComponentInChildren<Camera>(true);
            rootCanvas ??= movementController != null ? movementController.RootCanvas : FindObjectOfType<Canvas>();
        }

        private void Update()
        {
            if (WasKeyPressed(standUpKey) && IsSeated)
            {
                ExitSeat();
                return;
            }

            if (RuntimePanelUiUtility.IsComputerPanelOpen(rootCanvas))
            {
                return;
            }

            if (WasKeyPressed(interactKey))
            {
                TryInteract();
            }
        }

        public bool IsSeatedAt(SeatController seatController)
        {
            return seatController != null && currentSeat == seatController;
        }

        public bool TrySit(SeatController seatController)
        {
            if (seatController == null)
            {
                return false;
            }

            if (currentSeat == seatController)
            {
                return true;
            }

            if (currentSeat != null)
            {
                ExitSeat();
            }

            if (!seatController.TryOccupy(this, SeatOccupantType.Player))
            {
                return false;
            }

            currentSeat = seatController;
            movementController?.SetInteractionLock(true, false);
            Teleport(seatController.GetSeatPosition(), seatController.GetSeatRotation(), true);
            return true;
        }

        public void ExitSeat()
        {
            if (currentSeat == null)
            {
                return;
            }

            rootCanvas ??= movementController != null ? movementController.RootCanvas : FindObjectOfType<Canvas>();
            RuntimePanelUiUtility.SetComputerPanelActive(rootCanvas, false);

            var previousSeat = currentSeat;
            currentSeat = null;
            previousSeat.Vacate(this);
            Teleport(previousSeat.GetExitPosition(transform), previousSeat.GetExitRotation(transform), false);
            movementController?.SetInteractionLock(false, false);
        }

        private void TryInteract()
        {
            interactionCamera ??= movementController != null ? movementController.PlayerCamera : GetComponentInChildren<Camera>(true);
            if (interactionCamera == null)
            {
                return;
            }

            var ray = interactionCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            var triggerMode = includeTriggerColliders ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;
            if (!Physics.Raycast(ray, out var hit, interactionDistance, interactionMask, triggerMode))
            {
                return;
            }

            var interactable = FindInteractable(hit.transform);
            if (interactable == null || !interactable.CanInteract(this))
            {
                return;
            }

            interactable.Interact(this);
        }

        private void Teleport(Vector3 worldPosition, Quaternion worldRotation, bool resetLookPitch)
        {
            if (movementController != null)
            {
                movementController.SnapToPose(worldPosition, worldRotation, resetLookPitch);
                return;
            }

            transform.SetPositionAndRotation(worldPosition, worldRotation);
        }

        private static IInteractable FindInteractable(Transform target)
        {
            var current = target;
            while (current != null)
            {
                var behaviours = current.GetComponents<MonoBehaviour>();
                for (var i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] is IInteractable interactable)
                    {
                        return interactable;
                    }
                }

                current = current.parent;
            }

            return null;
        }

        private static bool WasKeyPressed(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                switch (key)
                {
                    case KeyCode.E: return keyboard.eKey.wasPressedThisFrame;
                    case KeyCode.Q: return keyboard.qKey.wasPressedThisFrame;
                }
            }
#endif
            return Input.GetKeyDown(key);
        }
    }
}
