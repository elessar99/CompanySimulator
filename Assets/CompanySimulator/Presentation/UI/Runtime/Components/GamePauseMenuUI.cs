using System.Collections.Generic;
using CompanySimulator.Features.Player.Runtime.Components;
using CompanySimulator.Features.Save.Runtime.Models;
using CompanySimulator.Features.Save.Runtime.Services;
using CompanySimulator.Presentation.UI.Runtime.Common;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CompanySimulator.Presentation.UI.Runtime.Components
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class GamePauseMenuUI : MonoBehaviour
    {
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private GameSaveManager saveManager;
        [SerializeField] private PlayerMovementController movementController;
        [SerializeField] private KeyCode toggleKey = KeyCode.Escape;

        private RectTransform rectTransform;
        private RectTransform contentRoot;
        private Text statusText;
        private Font defaultFont;
        private float previousTimeScale = 1f;
        private bool isOpen;
        private GameSaveSlotInfo selectedSlot;
        private string pendingDeleteSlotId;
        private Button overwriteSelectedButton;
        private Button loadSelectedButton;
        private Button deleteSelectedButton;
        private readonly List<Button> slotButtons = new List<Button>(16);
        private readonly Dictionary<Button, GameSaveSlotInfo> slotByButton = new Dictionary<Button, GameSaveSlotInfo>();
        private PauseMenuView currentView = PauseMenuView.Main;

        public static bool IsAnyOpen { get; private set; }
        public bool IsOpen => isOpen;

        private enum PauseMenuView
        {
            Main,
            Save,
            Load,
            Settings
        }

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            defaultFont = LoadDefaultFont();
            rootCanvas ??= GetComponentInParent<Canvas>();
            EnsureSaveManager();
            BuildRoot();
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!isOpen || !WasKeyPressed(toggleKey))
            {
                return;
            }

            if (currentView == PauseMenuView.Main)
            {
                CloseMenu();
                return;
            }

            ShowMainMenu();
        }

        public void SetRootCanvas(Canvas canvas)
        {
            if (canvas == null)
            {
                return;
            }

            rootCanvas = canvas;
            var hudRoot = RuntimePanelUiUtility.GetOrCreateHudRoot(rootCanvas);
            if (hudRoot != null && transform.parent != hudRoot)
            {
                transform.SetParent(hudRoot, false);
            }

            RuntimePanelUiUtility.EnsureResponsiveCanvasScaler(rootCanvas);
            BuildRoot();
        }

        public void SetMovementController(PlayerMovementController controller)
        {
            movementController = controller;
        }

        public bool TryOpen()
        {
            if (isOpen)
            {
                return true;
            }

            EnsureSaveManager();
            if (rootCanvas == null)
            {
                rootCanvas = FindObjectOfType<Canvas>();
            }

            SetRootCanvas(rootCanvas);
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            isOpen = true;
            IsAnyOpen = true;
            gameObject.SetActive(true);
            movementController ??= FindObjectOfType<PlayerMovementController>();
            movementController?.SetInteractionLock(true, true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ShowMainMenu();
            return true;
        }

        public void CloseMenu()
        {
            if (!isOpen)
            {
                return;
            }

            isOpen = false;
            IsAnyOpen = false;
            Time.timeScale = previousTimeScale;
            gameObject.SetActive(false);
            movementController?.SetInteractionLock(false, false);
            movementController?.RestoreGameplayCursorLock();
        }

        private void BuildRoot()
        {
            if (rectTransform == null)
            {
                rectTransform = (RectTransform)transform;
            }

            RuntimePanelUiUtility.StretchToParent(rectTransform, 0f, 0f, 0f, 0f);

            var background = GetComponent<Image>();
            if (background == null)
            {
                background = gameObject.AddComponent<Image>();
            }

            background.color = new Color(0.05f, 0.06f, 0.07f, 0.62f);

            if (contentRoot == null)
            {
                contentRoot = RuntimePanelUiUtility.CreateUiObject("PauseMenuContent", transform).GetComponent<RectTransform>();
            }

            ConfigureContentRoot();
        }

        private void ConfigureContentRoot()
        {
            if (contentRoot == null)
            {
                return;
            }

            contentRoot.anchorMin = new Vector2(0.25f, 0.5f);
            contentRoot.anchorMax = new Vector2(0.25f, 0.5f);
            contentRoot.pivot = new Vector2(0f, 0.5f);
            contentRoot.anchoredPosition = Vector2.zero;
            contentRoot.sizeDelta = new Vector2(720f, 680f);
        }

        private void ShowMainMenu()
        {
            currentView = PauseMenuView.Main;
            selectedSlot = null;
            pendingDeleteSlotId = null;
            RuntimePanelUiUtility.ClearChildren(contentRoot);
            ConfigureContentRoot();

            AddVerticalLayout(contentRoot, 22, TextAnchor.MiddleLeft);
            CreateMenuButton(contentRoot, "Oyuna Devam Et", CloseMenu, 36);
            CreateMenuButton(contentRoot, "Oyunu Kaydet", ShowSavePanel, 36);
            CreateMenuButton(contentRoot, "Oyunu Yükle", ShowLoadPanel, 36);
            CreateMenuButton(contentRoot, "Ayarlar", ShowSettingsPanel, 36);
            CreateMenuButton(contentRoot, "Oyundan Çık", QuitGame, 36);
            statusText = CreateStatusText(contentRoot);
        }

        private void ShowSavePanel()
        {
            currentView = PauseMenuView.Save;
            selectedSlot = null;
            pendingDeleteSlotId = null;
            BuildSlotPanel("Oyunu Kaydet", true);
        }

        private void ShowLoadPanel()
        {
            currentView = PauseMenuView.Load;
            selectedSlot = null;
            pendingDeleteSlotId = null;
            BuildSlotPanel("Oyunu Yükle", false);
        }

        private void ShowSettingsPanel()
        {
            currentView = PauseMenuView.Settings;
            RuntimePanelUiUtility.ClearChildren(contentRoot);
            ConfigureContentRoot();
            AddVerticalLayout(contentRoot, 22, TextAnchor.MiddleLeft);
            CreateLabel(contentRoot, "Ayarlar", 38, FontStyle.Bold);
            CreateLabel(contentRoot, "Ses ve görüntü ayarları sonraki aşamada eklenecek.", 24, FontStyle.Normal);
            CreateMenuButton(contentRoot, "Menüye Dön", ShowMainMenu, 30);
            statusText = CreateStatusText(contentRoot);
        }

        private void BuildSlotPanel(string title, bool isSavePanel)
        {
            RuntimePanelUiUtility.ClearChildren(contentRoot);
            ConfigureContentRoot();
            AddVerticalLayout(contentRoot, 14, TextAnchor.MiddleLeft);
            CreateLabel(contentRoot, title, 38, FontStyle.Bold);

            var slotListRoot = RuntimePanelUiUtility.CreateUiObject("SlotList", contentRoot).GetComponent<RectTransform>();
            slotListRoot.sizeDelta = new Vector2(0f, 390f);
            AddVerticalLayout(slotListRoot, 8, TextAnchor.UpperLeft);

            slotButtons.Clear();
            slotByButton.Clear();
            overwriteSelectedButton = null;
            loadSelectedButton = null;
            deleteSelectedButton = null;
            var slots = saveManager != null ? saveManager.RefreshSlots() : new List<GameSaveSlotInfo>();
            selectedSlot = FindMatchingSlot(slots, selectedSlot);
            if (slots.Count == 0)
            {
                CreateLabel(slotListRoot, "Henüz kayıt yok.", 24, FontStyle.Normal);
            }
            else
            {
                for (var i = 0; i < slots.Count; i++)
                {
                    CreateSlotButton(slotListRoot, slots[i]);
                }
            }

            if (isSavePanel)
            {
                CreateMenuButton(contentRoot, "Yeni Kayıt Oluştur", CreateNewSave, 28);
                overwriteSelectedButton = CreateMenuButton(contentRoot, "Seçili Kaydın Üstüne Kaydet", OverwriteSelectedSave, 28);
            }
            else
            {
                loadSelectedButton = CreateMenuButton(contentRoot, "Seçili Kaydı Yükle", LoadSelectedSave, 28);
            }

            deleteSelectedButton = CreateMenuButton(contentRoot, "Seçili Kaydı Sil", DeleteSelectedSave, 28);
            CreateMenuButton(contentRoot, "Menüye Dön", ShowMainMenu, 28);
            statusText = CreateStatusText(contentRoot);
            RefreshSlotSelection();
        }

        private void CreateNewSave()
        {
            if (saveManager != null && saveManager.TryCreateNewSave(out var slot, out var message))
            {
                selectedSlot = slot;
                BuildSlotPanel("Oyunu Kaydet", true);
                SetStatus(message);
                return;
            }

            SetStatus(saveManager != null ? "Kayıt oluşturulamadı." : "Kayıt sistemi bulunamadı.");
        }

        private void OverwriteSelectedSave()
        {
            if (saveManager == null)
            {
                SetStatus("Kayıt sistemi bulunamadı.");
                return;
            }

            if (saveManager.TryOverwriteSave(selectedSlot, out var message))
            {
                pendingDeleteSlotId = null;
                BuildSlotPanel("Oyunu Kaydet", true);
            }

            SetStatus(message);
        }

        private void LoadSelectedSave()
        {
            if (saveManager == null)
            {
                SetStatus("Kayıt sistemi bulunamadı.");
                return;
            }

            if (saveManager.TryLoadSave(selectedSlot, out var message))
            {
                pendingDeleteSlotId = null;
                CloseMenu();
                return;
            }

            SetStatus(message);
        }

        private void DeleteSelectedSave()
        {
            if (saveManager == null)
            {
                SetStatus("Kayıt sistemi bulunamadı.");
                return;
            }

            if (!CanDeleteSelectedSave())
            {
                SetStatus("Silinecek bir kayıt seçilmedi.");
                return;
            }

            var deleteKey = GetDeleteKey(selectedSlot);
            if (!string.Equals(pendingDeleteSlotId, deleteKey, System.StringComparison.Ordinal))
            {
                pendingDeleteSlotId = deleteKey;
                SetStatus("Seçili kaydı silmek için tekrar bas.");
                return;
            }

            var rebuildAsSavePanel = currentView == PauseMenuView.Save;
            if (saveManager.TryDeleteSave(selectedSlot, out var message))
            {
                selectedSlot = null;
                pendingDeleteSlotId = null;
                BuildSlotPanel(rebuildAsSavePanel ? "Oyunu Kaydet" : "Oyunu Yükle", rebuildAsSavePanel);
            }

            SetStatus(message);
        }

        private void CreateSlotButton(RectTransform parent, GameSaveSlotInfo slot)
        {
            var label = slot.IsCorrupt
                ? $"{slot.DisplayName} | Bozuk kayıt"
                : $"{slot.DisplayName} | Gün {slot.CurrentDay} | {slot.TimeLabel} | Para {slot.Balance:N0}";

            var button = CreateMenuButton(parent, label, null, 22);
            slotButtons.Add(button);
            slotByButton[button] = slot;
            button.onClick.AddListener(() =>
            {
                selectedSlot = slot;
                pendingDeleteSlotId = null;
                RefreshSlotSelection();
                SetStatus(slot.IsCorrupt ? slot.ErrorMessage : string.Empty);
            });
        }

        private void RefreshSlotSelection()
        {
            for (var i = 0; i < slotButtons.Count; i++)
            {
                var button = slotButtons[i];
                var image = button.GetComponent<Image>();
                if (image == null)
                {
                    continue;
                }

                slotByButton.TryGetValue(button, out var slot);
                image.color = slot == selectedSlot
                    ? new Color(1f, 1f, 1f, 0.18f)
                    : new Color(1f, 1f, 1f, 0.02f);
            }

            if (overwriteSelectedButton != null)
            {
                overwriteSelectedButton.interactable = CanUseSelectedSave();
            }

            if (loadSelectedButton != null)
            {
                loadSelectedButton.interactable = CanUseSelectedSave();
            }

            if (deleteSelectedButton != null)
            {
                deleteSelectedButton.interactable = CanDeleteSelectedSave();
            }
        }

        private Button CreateMenuButton(RectTransform parent, string label, UnityEngine.Events.UnityAction action, int fontSize)
        {
            var buttonObject = RuntimePanelUiUtility.CreateUiObject(label.Replace(" ", string.Empty), parent);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, 56f);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.02f);

            var button = buttonObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.04f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.16f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.24f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.02f);
            button.colors = colors;
            if (action != null)
            {
                button.onClick.AddListener(action);
            }

            var text = RuntimePanelUiUtility.CreateText(buttonObject.transform, defaultFont, label, fontSize, TextAnchor.MiddleLeft);
            text.color = Color.white;
            text.fontStyle = FontStyle.Bold;
            RuntimePanelUiUtility.StretchToParent(text.rectTransform, 18f, 4f, 18f, 4f);
            return button;
        }

        private Text CreateLabel(RectTransform parent, string label, int fontSize, FontStyle fontStyle)
        {
            var text = RuntimePanelUiUtility.CreateText(parent, defaultFont, label, fontSize, TextAnchor.MiddleLeft);
            text.color = Color.white;
            text.fontStyle = fontStyle;
            text.rectTransform.sizeDelta = new Vector2(0f, 58f);
            return text;
        }

        private Text CreateStatusText(RectTransform parent)
        {
            var text = RuntimePanelUiUtility.CreateText(parent, defaultFont, string.Empty, 20, TextAnchor.MiddleLeft);
            text.color = new Color(1f, 1f, 1f, 0.78f);
            text.rectTransform.sizeDelta = new Vector2(0f, 48f);
            return text;
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message ?? string.Empty;
            }
        }

        private static void AddVerticalLayout(RectTransform target, int spacing, TextAnchor alignment)
        {
            var layout = target.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = target.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.childAlignment = alignment;
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private void EnsureSaveManager()
        {
            if (saveManager == null)
            {
                saveManager = FindObjectOfType<GameSaveManager>();
            }

            if (saveManager == null)
            {
                saveManager = new GameObject("GameSaveManager", typeof(GameSaveManager)).GetComponent<GameSaveManager>();
            }
        }

        private static GameSaveSlotInfo FindMatchingSlot(IReadOnlyList<GameSaveSlotInfo> slots, GameSaveSlotInfo previousSelection)
        {
            if (slots == null || previousSelection == null)
            {
                return null;
            }

            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null)
                {
                    continue;
                }

                if ((!string.IsNullOrWhiteSpace(previousSelection.SlotId) &&
                     string.Equals(slot.SlotId, previousSelection.SlotId, System.StringComparison.Ordinal)) ||
                    (!string.IsNullOrWhiteSpace(previousSelection.FilePath) &&
                     string.Equals(slot.FilePath, previousSelection.FilePath, System.StringComparison.Ordinal)))
                {
                    return slot;
                }
            }

            return null;
        }

        private bool CanUseSelectedSave()
        {
            return selectedSlot != null &&
                   !selectedSlot.IsCorrupt &&
                   !string.IsNullOrWhiteSpace(selectedSlot.FilePath);
        }

        private bool CanDeleteSelectedSave()
        {
            return selectedSlot != null && !string.IsNullOrWhiteSpace(selectedSlot.FilePath);
        }

        private static string GetDeleteKey(GameSaveSlotInfo slot)
        {
            if (slot == null)
            {
                return string.Empty;
            }

            return !string.IsNullOrWhiteSpace(slot.FilePath) ? slot.FilePath : slot.SlotId;
        }

        private static Font LoadDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static bool WasKeyPressed(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                switch (key)
                {
                    case KeyCode.Escape:
                        return keyboard.escapeKey.wasPressedThisFrame;
                }
            }

            return false;
#else
            return Input.GetKeyDown(key);
#endif
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
