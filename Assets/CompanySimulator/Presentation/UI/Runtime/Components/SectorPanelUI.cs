using System;
using CompanySimulator.Features.Finance.Runtime.Components;
using CompanySimulator.Features.Finance.Runtime.Definitions;
using CompanySimulator.Features.Sectors.Runtime.Components;
using CompanySimulator.Features.Sectors.Runtime.Models;
using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CompanySimulator.Presentation.UI.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class SectorPanelUI : MonoBehaviour
    {
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private SectorManager sectorManager;
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private Vector2 panelSize = new Vector2(520f, 620f);

        private Font defaultFont;
        private GameObject panelRoot;
        private RectTransform contentRoot;
        private Text balanceText;
        private Text pageTitleText;
        private SectorRuntimeData selectedSector;

        private void Awake()
        {
            // Referanslar inspector'dan atanmadıysa sahneden otomatik bulunur.
            economyManager ??= FindObjectOfType<EconomyManager>();
            sectorManager ??= FindObjectOfType<SectorManager>();

            EnsureCanvas();
            EnsureEventSystem();
            defaultFont = LoadDefaultFont();
            BuildStaticUi();
        }

        private void OnEnable()
        {
            SubscribeEvents();
            RefreshAll();
        }

        private void Start()
        {
            RefreshAll();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        public void OpenPanel()
        {
            panelRoot.SetActive(true);
            selectedSector = null;
            ShowSectorList();
        }

        public void ClosePanel()
        {
            // Kapatırken her zaman ana sektör listesine dönülür.
            selectedSector = null;
            ShowSectorList();
            panelRoot.SetActive(false);
        }

        private void RefreshAll()
        {
            RefreshBalanceText();

            if (sectorManager == null)
            {
                return;
            }

            if (!sectorManager.IsInitialized)
            {
                sectorManager.Initialize();
            }

            if (selectedSector == null)
            {
                ShowSectorList();
                return;
            }

            if (sectorManager.TryGetSectorData(selectedSector.Sector, out var sectorData))
            {
                selectedSector = sectorData;
                ShowSectorDetails(sectorData);
            }
            else
            {
                selectedSector = null;
                ShowSectorList();
            }
        }

        private void SubscribeEvents()
        {
            if (economyManager != null)
            {
                economyManager.BalanceChanged -= HandleBalanceChanged;
                economyManager.BalanceChanged += HandleBalanceChanged;
            }

            if (sectorManager != null)
            {
                sectorManager.DataChanged -= HandleSectorDataChanged;
                sectorManager.DataChanged += HandleSectorDataChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            if (economyManager != null)
            {
                economyManager.BalanceChanged -= HandleBalanceChanged;
            }

            if (sectorManager != null)
            {
                sectorManager.DataChanged -= HandleSectorDataChanged;
            }
        }

        private void HandleBalanceChanged(Money _)
        {
            RefreshBalanceText();
        }

        private void HandleSectorDataChanged()
        {
            RefreshAll();
        }

        private void EnsureCanvas()
        {
            if (rootCanvas == null)
            {
                rootCanvas = FindObjectOfType<Canvas>();
            }

            if (rootCanvas != null)
            {
                return;
            }

            var canvasObject = new GameObject("MainCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            rootCanvas = canvasObject.GetComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private void BuildStaticUi()
        {
            CreateBalanceWidget();
            CreateOpenButton();
            CreatePanel();
            panelRoot.SetActive(false);
        }

        private void CreateBalanceWidget()
        {
            var balanceRoot = CreateUiObject("MoneyBar", rootCanvas.transform);
            var balanceRect = balanceRoot.GetComponent<RectTransform>();
            balanceRect.anchorMin = new Vector2(0f, 1f);
            balanceRect.anchorMax = new Vector2(0f, 1f);
            balanceRect.pivot = new Vector2(0f, 1f);
            balanceRect.anchoredPosition = new Vector2(20f, -20f);
            balanceRect.sizeDelta = new Vector2(280f, 48f);

            var background = balanceRoot.AddComponent<Image>();
            background.color = new Color(0.12f, 0.12f, 0.16f, 0.92f);

            balanceText = CreateText(balanceRoot.transform, "Para: 0", 22, TextAnchor.MiddleLeft);
            var textRect = balanceText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14f, 6f);
            textRect.offsetMax = new Vector2(-14f, -6f);
        }

        private void CreateOpenButton()
        {
            var button = CreateButton(rootCanvas.transform, "SectorsOpenButton", "Sektörler");
            var buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 1f);
            buttonRect.anchorMax = new Vector2(0f, 1f);
            buttonRect.pivot = new Vector2(0f, 1f);
            buttonRect.anchoredPosition = new Vector2(20f, -80f);
            buttonRect.sizeDelta = new Vector2(180f, 44f);
            button.onClick.AddListener(OpenPanel);
        }

        private void CreatePanel()
        {
            panelRoot = CreateUiObject("SectorPanel", rootCanvas.transform);
            var panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(0f, -10f);
            panelRect.sizeDelta = panelSize;

            var background = panelRoot.AddComponent<Image>();
            background.color = new Color(0.1f, 0.12f, 0.16f, 0.98f);

            var headerRoot = CreateUiObject("Header", panelRoot.transform);
            var headerRect = headerRoot.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0f, 70f);

            var headerBackground = headerRoot.AddComponent<Image>();
            headerBackground.color = new Color(0.17f, 0.21f, 0.29f, 1f);

            pageTitleText = CreateText(headerRoot.transform, "Sektörler", 28, TextAnchor.MiddleLeft);
            var titleRect = pageTitleText.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 0f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(18f, 8f);
            titleRect.offsetMax = new Vector2(-80f, -8f);

            var closeButton = CreateButton(headerRoot.transform, "CloseButton", "X");
            var closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-14f, 0f);
            closeRect.sizeDelta = new Vector2(50f, 40f);
            closeButton.onClick.AddListener(ClosePanel);

            var content = CreateUiObject("Content", panelRoot.transform);
            contentRoot = content.GetComponent<RectTransform>();
            contentRoot.anchorMin = new Vector2(0f, 0f);
            contentRoot.anchorMax = new Vector2(1f, 1f);
            contentRoot.offsetMin = new Vector2(16f, 16f);
            contentRoot.offsetMax = new Vector2(-16f, -86f);

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void ShowSectorList()
        {
            if (contentRoot == null)
            {
                return;
            }

            pageTitleText.text = "Sektörler";
            ClearChildren(contentRoot);

            if (sectorManager == null || !sectorManager.IsInitialized)
            {
                CreateInfoCard("Sektör sistemi henüz hazır değil.");
                return;
            }

            var sectors = sectorManager.Sectors;
            if (sectors.Count == 0)
            {
                CreateInfoCard("Henüz listelenecek sektör bulunmuyor.");
                return;
            }

            for (var i = 0; i < sectors.Count; i++)
            {
                CreateSectorButton(sectors[i]);
            }
        }

        private void ShowSectorDetails(SectorRuntimeData sectorData)
        {
            if (contentRoot == null || sectorData == null)
            {
                return;
            }

            pageTitleText.text = sectorData.Sector.DisplayName;
            ClearChildren(contentRoot);

            CreateInfoCard($"Toplam tamamlanan iş: {sectorData.CompletedProjectCount}");

            var availableProjects = sectorData.AvailableProjects;
            if (availableProjects.Count == 0)
            {
                CreateInfoCard("Bu sektör için henüz iş tanımı yok.");
            }
            else
            {
                for (var i = 0; i < availableProjects.Count; i++)
                {
                    CreateProjectCard(sectorData, availableProjects[i]);
                }
            }

            var newJobButton = CreateButton(contentRoot, "NewJobButton", "+ Yeni İş");
            var buttonRect = newJobButton.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(0f, 62f);
            newJobButton.onClick.AddListener(() => Debug.Log("Yeni iş başlatma ekranı daha sonra eklenecek.", this));
        }

        private void CreateSectorButton(SectorRuntimeData sectorData)
        {
            var label = $"{sectorData.Sector.DisplayName}\nTamamlanan İş: {sectorData.CompletedProjectCount}";
            var button = CreateButton(contentRoot, $"Sector_{sectorData.Sector.Id}", label);
            var buttonRect = button.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(0f, 72f);
            button.onClick.AddListener(() =>
            {
                selectedSector = sectorData;
                ShowSectorDetails(sectorData);
            });
        }

        private void CreateProjectCard(SectorRuntimeData sectorData, ProjectExecutionDefinition project)
        {
            var completedCount = sectorData.GetCompletedCount(project);
            var button = CreateButton(contentRoot, $"Project_{project.Id}", $"{project.DisplayName}\nTamamlanma: {completedCount}");
            var buttonRect = button.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(0f, 72f);
            button.onClick.AddListener(() => Debug.Log($"{project.DisplayName} detay ekranı daha sonra eklenecek.", this));
        }

        private void CreateInfoCard(string message)
        {
            var infoRoot = CreateUiObject("InfoCard", contentRoot);
            var rect = infoRoot.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, 58f);

            var image = infoRoot.AddComponent<Image>();
            image.color = new Color(0.18f, 0.2f, 0.25f, 1f);

            var text = CreateText(infoRoot.transform, message, 20, TextAnchor.MiddleLeft);
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14f, 6f);
            textRect.offsetMax = new Vector2(-14f, -6f);
        }

        private GameObject CreateUiObject(string objectName, Transform parent)
        {
            var uiObject = new GameObject(objectName, typeof(RectTransform));
            uiObject.transform.SetParent(parent, false);
            return uiObject;
        }

        private Button CreateButton(Transform parent, string objectName, string label)
        {
            var buttonObject = CreateUiObject(objectName, parent);
            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.23f, 0.3f, 0.42f, 1f);

            var button = buttonObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(0.23f, 0.3f, 0.42f, 1f);
            colors.highlightedColor = new Color(0.3f, 0.38f, 0.52f, 1f);
            colors.pressedColor = new Color(0.18f, 0.24f, 0.35f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);
            button.colors = colors;

            var text = CreateText(buttonObject.transform, label, 22, TextAnchor.MiddleLeft);
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 8f);
            textRect.offsetMax = new Vector2(-16f, -8f);

            return button;
        }

        private Text CreateText(Transform parent, string value, int fontSize, TextAnchor anchor)
        {
            var textObject = CreateUiObject("Text", parent);
            var text = textObject.AddComponent<Text>();
            text.font = defaultFont;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private void RefreshBalanceText()
        {
            if (balanceText == null)
            {
                return;
            }

            var balanceAmount = economyManager != null ? economyManager.Balance.Amount : 0L;
            balanceText.text = $"Para: {balanceAmount:N0}";
        }

        private Font LoadDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private void ClearChildren(RectTransform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }
    }
}
