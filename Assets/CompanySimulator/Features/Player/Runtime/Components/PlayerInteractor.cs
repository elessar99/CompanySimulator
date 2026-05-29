using CompanySimulator.Features.Furniture.Runtime.Components;
using CompanySimulator.Features.Furniture.Runtime.Interactions;
using CompanySimulator.Features.Npcs.Runtime.Agents;
using CompanySimulator.Presentation.UI.Runtime.Common;
using CompanySimulator.Presentation.UI.Runtime.Components;
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
        [SerializeField] private FurniturePlacementManager furniturePlacementManager;
        [SerializeField] private InteractionPromptUI interactionPromptUi;
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private GamePauseMenuUI pauseMenuUi;
        [SerializeField, Min(0.5f)] private float interactionDistance = 4f;
        [SerializeField] private LayerMask interactionMask = Physics.DefaultRaycastLayers;
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private KeyCode standUpKey = KeyCode.Q;
        [SerializeField] private KeyCode buildModeKey = KeyCode.B;
        [SerializeField] private KeyCode rotatePlacementKey = KeyCode.R;
        [SerializeField] private KeyCode cancelKey = KeyCode.Escape;
        [SerializeField, Min(0.5f)] private float agentDismissDistance = 2f;
        [SerializeField] private bool includeTriggerColliders = true;

        private SeatController currentSeat;

        public bool IsSeated => currentSeat != null;
        public SeatController CurrentSeat => currentSeat;
        public Canvas RootCanvas => rootCanvas;
        public PlayerMovementController MovementController => movementController;

        private void Awake()
        {
            movementController ??= GetComponent<PlayerMovementController>();
            interactionCamera ??= movementController != null ? movementController.PlayerCamera : GetComponentInChildren<Camera>(true);
            furniturePlacementManager ??= FindObjectOfType<FurniturePlacementManager>();
            if (furniturePlacementManager == null)
            {
                furniturePlacementManager = new GameObject("FurniturePlacementManager", typeof(FurniturePlacementManager)).GetComponent<FurniturePlacementManager>();
            }

            rootCanvas ??= movementController != null ? movementController.RootCanvas : FindObjectOfType<Canvas>();
            EnsureInteractionPromptUi();
            EnsurePauseMenuUi();
        }

        private void Update()
        {
            rootCanvas ??= movementController != null ? movementController.RootCanvas : FindObjectOfType<Canvas>();
            EnsureInteractionPromptUi();
            EnsurePauseMenuUi();
            var computerOpen = RuntimePanelUiUtility.IsComputerPanelOpen(rootCanvas);

            if (pauseMenuUi != null && pauseMenuUi.IsOpen)
            {
                ClearInteractionPrompt();
                return;
            }

            if (WasKeyPressed(cancelKey))
            {
                if (HandleCancelInput(computerOpen))
                {
                    return;
                }

                if (!computerOpen && pauseMenuUi != null && pauseMenuUi.TryOpen())
                {
                    ClearInteractionPrompt();
                    return;
                }
            }

            if (WasKeyPressed(standUpKey) && IsSeated)
            {
                if (furniturePlacementManager != null && furniturePlacementManager.IsBuildModeActive)
                {
                    furniturePlacementManager.SetBuildMode(false);
                }

                ExitSeat();
                return;
            }

            if (WasKeyPressed(buildModeKey))
            {
                ToggleBuildMode(computerOpen);
            }

            UpdateBuildModeCursorState();

            if (computerOpen)
            {
                ClearInteractionPrompt();
                if (furniturePlacementManager != null && furniturePlacementManager.IsBuildModeActive)
                {
                    furniturePlacementManager.SetBuildMode(false);
                    UpdateBuildModeCursorState();
                }

                return;
            }

            if (furniturePlacementManager != null && furniturePlacementManager.IsBuildModeActive)
            {
                ClearInteractionPrompt();
                if (IsSeated)
                {
                    furniturePlacementManager.SetBuildMode(false);
                    UpdateBuildModeCursorState();
                    return;
                }

                if (furniturePlacementManager.HasPendingPlacement)
                {
                    UpdatePlacementPreview();

                    if (WasKeyPressed(standUpKey))
                    {
                        furniturePlacementManager.CancelPlacement();
                        UpdateBuildModeCursorState();
                        return;
                    }

                    if (WasKeyPressed(rotatePlacementKey))
                    {
                        furniturePlacementManager.RotatePendingPlacement(15f);
                        UpdatePlacementPreview();
                    }

                    if (WasKeyPressed(interactKey))
                    {
                        TryPlaceFurniture();
                        UpdateBuildModeCursorState();
                    }
                }

                return;
            }

            UpdateInteractionPrompt();

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
            Teleport(seatController.GetSeatPosition(SeatOccupantType.Player), seatController.GetSeatRotation(SeatOccupantType.Player), true);
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
            movementController?.RestoreGameplayCursorLock();
        }

        private void TryInteract()
        {
            interactionCamera ??= movementController != null ? movementController.PlayerCamera : GetComponentInChildren<Camera>(true);
            if (interactionCamera == null)
            {
                ClearInteractionPrompt();
                return;
            }

            var ray = interactionCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            var triggerMode = includeTriggerColliders ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;
            if (!Physics.Raycast(ray, out var hit, interactionDistance, interactionMask, triggerMode))
            {
                ClearInteractionPrompt();
                return;
            }

            var agentInteractable = FindDetectedAgentInteractable(hit.transform);
            if (agentInteractable != null && hit.distance <= agentDismissDistance && agentInteractable.CanInteract(this))
            {
                agentInteractable.Interact(this);
                return;
            }

            var interactable = FindInteractable(hit.transform);
            if (interactable != null && interactable.CanInteract(this))
            {
                interactable.Interact(this);
                return;
            }

            var playerSeat = FindPlayerSeat(hit.transform);
            if (playerSeat != null && !IsSeatedAt(playerSeat))
            {
                TrySit(playerSeat);
                return;
            }

            var ceoDesk = hit.transform.GetComponentInParent<CeoDeskController>();
            if (ceoDesk != null && ceoDesk.PlayerSeat != null && !IsSeatedAt(ceoDesk.PlayerSeat))
            {
                TrySit(ceoDesk.PlayerSeat);
            }
        }

        private void TryPlaceFurniture()
        {
            interactionCamera ??= movementController != null ? movementController.PlayerCamera : GetComponentInChildren<Camera>(true);
            if (interactionCamera == null || furniturePlacementManager == null)
            {
                return;
            }

            var ray = interactionCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            furniturePlacementManager.TryPlaceFromRay(ray, transform.forward, out _);
        }

        private void UpdatePlacementPreview()
        {
            interactionCamera ??= movementController != null ? movementController.PlayerCamera : GetComponentInChildren<Camera>(true);
            if (interactionCamera == null || furniturePlacementManager == null || !furniturePlacementManager.HasPendingPlacement)
            {
                return;
            }

            var ray = interactionCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            furniturePlacementManager.UpdatePreviewFromRay(ray, transform.forward, out _);
        }

        private void UpdateInteractionPrompt()
        {
            interactionCamera ??= movementController != null ? movementController.PlayerCamera : GetComponentInChildren<Camera>(true);
            if (interactionCamera == null)
            {
                ClearInteractionPrompt();
                return;
            }

            var ray = interactionCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            var triggerMode = includeTriggerColliders ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;
            if (!Physics.Raycast(ray, out var hit, interactionDistance, interactionMask, triggerMode))
            {
                ClearInteractionPrompt();
                return;
            }

            var interactable = ResolveFocusedInteractable(hit);
            if (interactable == null)
            {
                ClearInteractionPrompt();
                return;
            }

            SetInteractionPrompt(interactable.GetInteractionText(this));
        }

        private void ToggleBuildMode(bool computerOpen)
        {
            if (IsSeated || computerOpen || furniturePlacementManager == null)
            {
                return;
            }

            furniturePlacementManager.ToggleBuildMode();
        }

        private void UpdateBuildModeCursorState()
        {
            movementController?.SetCursorUnlockOverride(
                furniturePlacementManager != null &&
                furniturePlacementManager.IsBuildModeActive &&
                !furniturePlacementManager.HasPendingPlacement &&
                !IsSeated);
        }

        private bool HandleCancelInput(bool computerOpen)
        {
            if (furniturePlacementManager != null && furniturePlacementManager.IsBuildModeActive)
            {
                furniturePlacementManager.SetBuildMode(false);
                UpdateBuildModeCursorState();
                return true;
            }

            if (!computerOpen)
            {
                return false;
            }

            rootCanvas ??= movementController != null ? movementController.RootCanvas : FindObjectOfType<Canvas>();
            if (RuntimePanelUiUtility.TryCloseTopComputerWindow(rootCanvas))
            {
                return true;
            }

            RuntimePanelUiUtility.SetComputerPanelActive(rootCanvas, false);
            movementController?.RestoreGameplayCursorLock();
            return true;
        }

        private IInteractable ResolveFocusedInteractable(RaycastHit hit)
        {
            var agentInteractable = FindDetectedAgentInteractable(hit.transform);
            if (agentInteractable != null && hit.distance <= agentDismissDistance)
            {
                return agentInteractable;
            }

            return FindInteractable(hit.transform);
        }

        private void EnsureInteractionPromptUi()
        {
            if (rootCanvas == null)
            {
                return;
            }

            if (interactionPromptUi == null)
            {
                interactionPromptUi = FindObjectOfType<InteractionPromptUI>();
            }

            if (interactionPromptUi == null)
            {
                interactionPromptUi = new GameObject("InteractionPromptUI", typeof(InteractionPromptUI)).GetComponent<InteractionPromptUI>();
            }

            interactionPromptUi.SetRootCanvas(rootCanvas);
        }

        private void EnsurePauseMenuUi()
        {
            if (rootCanvas == null)
            {
                return;
            }

            if (pauseMenuUi == null)
            {
                pauseMenuUi = FindObjectOfType<GamePauseMenuUI>(true);
            }

            if (pauseMenuUi == null)
            {
                pauseMenuUi = new GameObject("GamePauseMenuUI", typeof(GamePauseMenuUI)).GetComponent<GamePauseMenuUI>();
            }

            pauseMenuUi.SetRootCanvas(rootCanvas);
            if (movementController != null)
            {
                pauseMenuUi.SetMovementController(movementController);
            }
        }

        private void SetInteractionPrompt(string message)
        {
            EnsureInteractionPromptUi();
            interactionPromptUi?.SetPrompt(message);
        }

        private void ClearInteractionPrompt()
        {
            interactionPromptUi?.SetPrompt(string.Empty);
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

        private static SeatController FindPlayerSeat(Transform target)
        {
            var current = target;
            while (current != null)
            {
                var seat = current.GetComponent<SeatController>();
                if (seat != null && seat.SeatPoint != null && seat.SeatPoint.AllowedOccupantType == SeatOccupantType.Player)
                {
                    return seat;
                }

                current = current.parent;
            }

            return null;
        }

        private static DetectedAgentInteractable FindDetectedAgentInteractable(Transform target)
        {
            var current = target;
            while (current != null)
            {
                var interactable = current.GetComponent<DetectedAgentInteractable>();
                if (interactable != null)
                {
                    return interactable;
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
                    case KeyCode.B: return keyboard.bKey.wasPressedThisFrame;
                    case KeyCode.E: return keyboard.eKey.wasPressedThisFrame;
                    case KeyCode.Escape: return keyboard.escapeKey.wasPressedThisFrame;
                    case KeyCode.J: return keyboard.jKey.wasPressedThisFrame;
                    case KeyCode.Q: return keyboard.qKey.wasPressedThisFrame;
                    case KeyCode.R: return keyboard.rKey.wasPressedThisFrame;
                }
            }
            return false;
#else
            return Input.GetKeyDown(key);
#endif
        }
    }
}
