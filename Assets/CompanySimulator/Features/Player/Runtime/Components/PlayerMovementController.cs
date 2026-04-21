using CompanySimulator.Features.Finance.Runtime.Components;
using CompanySimulator.Features.Furniture.Runtime.Components;
using CompanySimulator.Presentation.UI.Runtime.Common;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CompanySimulator.Features.Player.Runtime.Components
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovementController : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform cameraRoot;
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private FurniturePlacementManager furniturePlacementManager;
        [SerializeField, Min(0.1f)] private float walkSpeed = 3.5f;
        [SerializeField, Min(0.1f)] private float runSpeed = 6f;
        [SerializeField, Min(0f)] private float acceleration = 14f;
        [SerializeField, Min(0f)] private float airControl = 0.35f;
        [SerializeField, Min(0f)] private float gravity = 25f;
        [SerializeField, Min(0f)] private float groundedForce = 2f;
        [SerializeField, Range(0.01f, 20f)] private float lookSensitivity = 2f;
        [SerializeField, Range(0f, 89f)] private float maxLookAngle = 80f;
        [SerializeField] private bool lockCursorOnStart = true;
        [SerializeField] private KeyCode runKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode cursorToggleKey = KeyCode.Escape;
        [SerializeField] private KeyCode computerToggleKey = KeyCode.T;
        [SerializeField] private KeyCode advanceDayKey = KeyCode.G;

        private CharacterController characterController;
        private Vector3 planarVelocity;
        private float verticalVelocity;
        private float pitch;
        private bool cursorLocked;
        private bool wantsCursorLocked;
        private bool externalMovementLock;
        private bool externalCursorUnlock;

        public Camera PlayerCamera => playerCamera;
        public Transform LookOrigin => playerCamera != null ? playerCamera.transform : cameraRoot;
        public Canvas RootCanvas => rootCanvas;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerCamera ??= GetComponentInChildren<Camera>(true);
            cameraRoot ??= playerCamera != null ? playerCamera.transform : null;
            economyManager ??= FindObjectOfType<EconomyManager>();
            furniturePlacementManager ??= FindObjectOfType<FurniturePlacementManager>();
            rootCanvas ??= FindObjectOfType<Canvas>();

            if (computerToggleKey == KeyCode.R)
            {
                computerToggleKey = KeyCode.T;
            }

            if (cameraRoot != null)
            {
                pitch = NormalizePitch(cameraRoot.localEulerAngles.x);
            }
        }

        private void Start()
        {
            wantsCursorLocked = lockCursorOnStart;
            SetCursorLocked(wantsCursorLocked);
        }

        private void Update()
        {
            HandleHotkeys();

            var computerOpen = IsComputerOpen();
            if (computerOpen)
            {
                wantsCursorLocked = false;
            }

            SetCursorLocked(computerOpen || externalCursorUnlock ? false : wantsCursorLocked);

            if (!computerOpen && cursorLocked)
            {
                HandleLook();
            }

            if (computerOpen || externalMovementLock || !cursorLocked)
            {
                HandleStationaryMovement();
                return;
            }

            HandleMovement();
        }

        private void HandleHotkeys()
        {
            if (WasKeyPressed(computerToggleKey) && (furniturePlacementManager == null || !furniturePlacementManager.IsBuildModeActive))
            {
                rootCanvas ??= FindObjectOfType<Canvas>();
                var isOpen = RuntimePanelUiUtility.ToggleComputerPanel(rootCanvas);
                wantsCursorLocked = !isOpen;
            }

            if (WasKeyPressed(advanceDayKey))
            {
                economyManager ??= FindObjectOfType<EconomyManager>();
                if (economyManager != null)
                {
                    economyManager.AdvanceDay();
                }
            }
        }

        private void HandleLook()
        {
            if (!cursorLocked)
            {
                return;
            }

            var lookInput = ReadLookInput() * lookSensitivity;
            var mouseX = lookInput.x;
            var mouseY = lookInput.y;

            transform.Rotate(Vector3.up * mouseX, Space.Self);

            if (cameraRoot == null)
            {
                return;
            }

            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);
            cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void HandleMovement()
        {
            var moveInput = ReadMovementInput();
            moveInput = Vector2.ClampMagnitude(moveInput, 1f);

            var targetSpeed = IsRunHeld() ? runSpeed : walkSpeed;
            var targetPlanarVelocity = (transform.right * moveInput.x + transform.forward * moveInput.y) * targetSpeed;
            var control = characterController.isGrounded ? acceleration : acceleration * airControl;
            planarVelocity = Vector3.MoveTowards(planarVelocity, targetPlanarVelocity, control * UnityEngine.Time.deltaTime);

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -groundedForce;
            }
            else
            {
                verticalVelocity -= gravity * UnityEngine.Time.deltaTime;
            }

            var movement = planarVelocity + Vector3.up * verticalVelocity;
            characterController.Move(movement * UnityEngine.Time.deltaTime);
        }

        private void HandleStationaryMovement()
        {
            planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, acceleration * UnityEngine.Time.deltaTime);

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -groundedForce;
            }
            else
            {
                verticalVelocity -= gravity * UnityEngine.Time.deltaTime;
            }

            characterController.Move(Vector3.up * verticalVelocity * UnityEngine.Time.deltaTime);
        }

        private void SetCursorLocked(bool isLocked)
        {
            var expectedLockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
            var expectedVisibility = !isLocked;
            if (cursorLocked == isLocked && Cursor.lockState == expectedLockState && Cursor.visible == expectedVisibility)
            {
                return;
            }

            cursorLocked = isLocked;
            Cursor.lockState = expectedLockState;
            Cursor.visible = expectedVisibility;
        }

        public void SetInteractionLock(bool isLocked, bool unlockCursor)
        {
            externalMovementLock = isLocked;
            externalCursorUnlock = isLocked && unlockCursor;
        }

        public void SetCursorUnlockOverride(bool unlockCursor)
        {
            externalCursorUnlock = unlockCursor;
        }

        public void RestoreGameplayCursorLock()
        {
            wantsCursorLocked = true;
            externalCursorUnlock = false;

            if (!IsComputerOpen())
            {
                SetCursorLocked(true);
            }
        }

        public void FocusViewAt(Vector3 worldTarget)
        {
            if (cameraRoot == null)
            {
                return;
            }

            var lookOrigin = LookOrigin != null ? LookOrigin.position : transform.position;
            var toTarget = worldTarget - lookOrigin;
            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            var planarDirection = Vector3.ProjectOnPlane(toTarget, Vector3.up);
            if (planarDirection.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(planarDirection.normalized, Vector3.up);
            }

            var horizontalDistance = planarDirection.magnitude;
            pitch = -Mathf.Atan2(toTarget.y, Mathf.Max(0.001f, horizontalDistance)) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);
            cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        public void SnapToPose(Vector3 worldPosition, Quaternion worldRotation, bool resetLookPitch)
        {
            var wasEnabled = characterController != null && characterController.enabled;
            if (wasEnabled)
            {
                characterController.enabled = false;
            }

            transform.SetPositionAndRotation(worldPosition, worldRotation);

            if (cameraRoot != null)
            {
                if (resetLookPitch)
                {
                    pitch = 0f;
                    cameraRoot.localRotation = Quaternion.identity;
                }
                else
                {
                    pitch = NormalizePitch(cameraRoot.localEulerAngles.x);
                }
            }

            if (wasEnabled)
            {
                characterController.enabled = true;
            }
        }

        private bool IsComputerOpen()
        {
            rootCanvas ??= FindObjectOfType<Canvas>();
            return RuntimePanelUiUtility.IsComputerPanelOpen(rootCanvas);
        }

        private static Vector2 ReadMovementInput()
        {
            var horizontal = 0f;
            var vertical = 0f;

            if (IsKeyHeld(KeyCode.A) || IsKeyHeld(KeyCode.LeftArrow)) horizontal -= 1f;
            if (IsKeyHeld(KeyCode.D) || IsKeyHeld(KeyCode.RightArrow)) horizontal += 1f;
            if (IsKeyHeld(KeyCode.S) || IsKeyHeld(KeyCode.DownArrow)) vertical -= 1f;
            if (IsKeyHeld(KeyCode.W) || IsKeyHeld(KeyCode.UpArrow)) vertical += 1f;

            return new Vector2(horizontal, vertical);
        }

        private static Vector2 ReadLookInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                return Mouse.current.delta.ReadValue() * 0.01f;
            }
            return Vector2.zero;
#else
            return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
#endif
        }

        private bool IsRunHeld()
        {
            return IsKeyHeld(runKey);
        }

        private static bool WasKeyPressed(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                switch (key)
                {
                    case KeyCode.W: return keyboard.wKey.wasPressedThisFrame;
                    case KeyCode.A: return keyboard.aKey.wasPressedThisFrame;
                    case KeyCode.S: return keyboard.sKey.wasPressedThisFrame;
                    case KeyCode.D: return keyboard.dKey.wasPressedThisFrame;
                    case KeyCode.UpArrow: return keyboard.upArrowKey.wasPressedThisFrame;
                    case KeyCode.DownArrow: return keyboard.downArrowKey.wasPressedThisFrame;
                    case KeyCode.LeftArrow: return keyboard.leftArrowKey.wasPressedThisFrame;
                    case KeyCode.RightArrow: return keyboard.rightArrowKey.wasPressedThisFrame;
                    case KeyCode.LeftShift: return keyboard.leftShiftKey.wasPressedThisFrame;
                    case KeyCode.RightShift: return keyboard.rightShiftKey.wasPressedThisFrame;
                    case KeyCode.Escape: return keyboard.escapeKey.wasPressedThisFrame;
                    case KeyCode.R: return keyboard.rKey.wasPressedThisFrame;
                    case KeyCode.T: return keyboard.tKey.wasPressedThisFrame;
                    case KeyCode.G: return keyboard.gKey.wasPressedThisFrame;
                }
            }
            return false;
#else
            return Input.GetKeyDown(key);
#endif
        }

        private static bool IsKeyHeld(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                switch (key)
                {
                    case KeyCode.W: return keyboard.wKey.isPressed;
                    case KeyCode.A: return keyboard.aKey.isPressed;
                    case KeyCode.S: return keyboard.sKey.isPressed;
                    case KeyCode.D: return keyboard.dKey.isPressed;
                    case KeyCode.UpArrow: return keyboard.upArrowKey.isPressed;
                    case KeyCode.DownArrow: return keyboard.downArrowKey.isPressed;
                    case KeyCode.LeftArrow: return keyboard.leftArrowKey.isPressed;
                    case KeyCode.RightArrow: return keyboard.rightArrowKey.isPressed;
                    case KeyCode.LeftShift: return keyboard.leftShiftKey.isPressed;
                    case KeyCode.RightShift: return keyboard.rightShiftKey.isPressed;
                    case KeyCode.Escape: return keyboard.escapeKey.isPressed;
                    case KeyCode.R: return keyboard.rKey.isPressed;
                    case KeyCode.T: return keyboard.tKey.isPressed;
                    case KeyCode.G: return keyboard.gKey.isPressed;
                }
            }
            return false;
#else
            return Input.GetKey(key);
#endif
        }

        private static float NormalizePitch(float angle)
        {
            if (angle > 180f)
            {
                angle -= 360f;
            }

            return angle;
        }
    }
}
