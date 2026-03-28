using System.Text;
using CompanySimulator.Features.Finance.Runtime.Components;
using CompanySimulator.Features.Rivals.Runtime.Components;
using CompanySimulator.Features.Rivals.Runtime.Models;
using CompanySimulator.Features.Sectors.Runtime.Components;
using CompanySimulator.Features.Sectors.Runtime.Services;
using CompanySimulator.Presentation.UI.Runtime.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CompanySimulator.Presentation.UI.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class DebugPanelUI : MonoBehaviour
    {
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private SectorManager sectorManager;
        [SerializeField] private RivalCompanyManager rivalCompanyManager;
        [SerializeField] private SectorPanelUI sectorPanelUI;
        [SerializeField] private EmployeePanelUI employeePanelUI;
        [SerializeField] private AccountingPanelUI accountingPanelUI;
        [SerializeField] private BankPanelUI bankPanelUI;
        [SerializeField] private FinanceOverviewPanelUI financeOverviewPanelUI;
        [SerializeField] private RivalCompanyPanelUI rivalCompanyPanelUI;
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private Vector2 panelSize = new Vector2(820f, 720f);

        private Font defaultFont;
        private GameObject panelRoot;
        private RectTransform contentRoot;
        private Text pageTitleText;
        private int activeTab;

        private static readonly StringBuilder SharedBuilder = new StringBuilder(512);

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            economyManager ??= FindObjectOfType<EconomyManager>();
            sectorManager ??= FindObjectOfType<SectorManager>();
            rivalCompanyManager ??= FindObjectOfType<RivalCompanyManager>();
            sectorPanelUI ??= FindObjectOfType<SectorPanelUI>();
            employeePanelUI ??= FindObjectOfType<EmployeePanelUI>();
            accountingPanelUI ??= FindObjectOfType<AccountingPanelUI>();
            bankPanelUI ??= FindObjectOfType<BankPanelUI>();
            financeOverviewPanelUI ??= FindObjectOfType<FinanceOverviewPanelUI>();
            rivalCompanyPanelUI ??= FindObjectOfType<RivalCompanyPanelUI>();
            EnsureCanvas();
            EnsureEventSystem();
            defaultFont = LoadDefaultFont();
            BuildUi();
        }

        private void OnEnable()
        {
            if (sectorManager != null)
            {
                sectorManager.DataChanged -= RefreshPage;
                sectorManager.DataChanged += RefreshPage;
            }

            if (rivalCompanyManager != null)
            {
                rivalCompanyManager.DataChanged -= RefreshPage;
                rivalCompanyManager.DataChanged += RefreshPage;
            }

            if (economyManager != null)
            {
                economyManager.DayAdvanced -= OnDayAdvanced;
                economyManager.DayAdvanced += OnDayAdvanced;
            }

            RefreshPage();
        }

        private void OnDisable()
        {
            if (sectorManager != null)
            {
                sectorManager.DataChanged -= RefreshPage;
            }

            if (rivalCompanyManager != null)
            {
                rivalCompanyManager.DataChanged -= RefreshPage;
            }

            if (economyManager != null)
            {
                economyManager.DayAdvanced -= OnDayAdvanced;
            }
        }

        private void OnDayAdvanced(int day)
        {
            RefreshPage();
        }

        public void OpenPanel()
        {
            if (sectorPanelUI != null && sectorPanelUI.IsOpen) sectorPanelUI.ClosePanel();
            if (employeePanelUI != null && employeePanelUI.IsOpen) employeePanelUI.ClosePanel();
            if (accountingPanelUI != null && accountingPanelUI.IsOpen) accountingPanelUI.ClosePanel();
            if (bankPanelUI != null && bankPanelUI.IsOpen) bankPanelUI.ClosePanel();
            if (financeOverviewPanelUI != null && financeOverviewPanelUI.IsOpen) financeOverviewPanelUI.ClosePanel();
            if (rivalCompanyPanelUI != null && rivalCompanyPanelUI.IsOpen) rivalCompanyPanelUI.ClosePanel();

            panelRoot.SetActive(true);
            RefreshPage();
        }

        public void ClosePanel()
        {
            panelRoot.SetActive(false);
        }

        private void RefreshPage()
        {
            if (contentRoot == null)
            {
                return;
            }

            RuntimePanelUiUtility.ClearChildren(contentRoot);

            if (activeTab == 0)
            {
                pageTitleText.text = "Debug — Sektör İstatistikleri";
                RenderSectorTab();
            }
            else
            {
                pageTitleText.text = "Debug — Şirket İstatistikleri";
                RenderRivalTab();
            }
        }

        private void RenderSectorTab()
        {
            if (sectorManager == null || !sectorManager.IsInitialized)
            {
                CreateInfoCard("Sektör verisi yüklenmedi.", 48f);
                return;
            }

            var sectors = sectorManager.Sectors;
            if (sectors.Count == 0)
            {
                CreateInfoCard("Tanımlı sektör yok.", 48f);
                return;
            }

            for (var i = 0; i < sectors.Count; i++)
            {
                var sectorData = sectors[i];
                var sector = sectorData.Sector;
                if (sector == null)
                {
                    continue;
                }

                var cachedCount = SectorCompetitionService.GetCachedProjectCount(sector);
                var lingeringCount = SectorCompetitionService.GetLingeringCount(sector);
                var revenueMultiplier = SectorCompetitionService.GetCachedRevenueMultiplier(sector);

                SharedBuilder.Clear();
                SharedBuilder.Append("<b>");
                SharedBuilder.Append(sector.DisplayName);
                SharedBuilder.Append("</b>");
                SharedBuilder.Append("\nToplam Proje (cache): ");
                SharedBuilder.Append(cachedCount);
                SharedBuilder.Append("  |  Oyuncu Aktif: ");
                SharedBuilder.Append(sectorData.ActiveProjectCount);
                SharedBuilder.Append("  |  Lingering: ");
                SharedBuilder.Append(lingeringCount);
                SharedBuilder.Append("\nGelir Düşme Oranı: ");
                SharedBuilder.Append((revenueMultiplier * 100f).ToString("F1"));
                SharedBuilder.Append("% (çarpan: ");
                SharedBuilder.Append(revenueMultiplier.ToString("F3"));
                SharedBuilder.Append(")");

                var text = CreateInfoCard(SharedBuilder.ToString(), 80f);
                text.supportRichText = true;
            }
        }

        private void RenderRivalTab()
        {
            if (rivalCompanyManager == null || !rivalCompanyManager.IsInitialized)
            {
                CreateInfoCard("Rakip şirket verisi yüklenmedi.", 48f);
                return;
            }

            var rivals = rivalCompanyManager.Rivals;
            if (rivals.Count == 0)
            {
                CreateInfoCard("Tanımlı rakip şirket yok.", 48f);
                return;
            }

            for (var i = 0; i < rivals.Count; i++)
            {
                RenderRivalDebugCard(rivals[i]);
            }
        }

        private void RenderRivalDebugCard(RivalCompanyRuntimeData rival)
        {
            var definition = rival.Definition;

            SharedBuilder.Clear();
            SharedBuilder.Append("<b>");
            SharedBuilder.Append(definition.DisplayName);
            SharedBuilder.Append("</b>");
            SharedBuilder.Append("  |  Bakiye: ");
            SharedBuilder.Append(rival.Balance.Amount.ToString("N0"));
            SharedBuilder.Append("  |  Aktif İş: ");
            SharedBuilder.Append(rival.ActiveJobCount);
            SharedBuilder.Append("\nSatış Çarpanı: ");
            SharedBuilder.Append(definition.SellDesireMultiplier.ToString("F2"));

            var jobs = definition.AvailableJobs;
            for (var j = 0; j < jobs.Count; j++)
            {
                var job = jobs[j];
                if (job == null || job.Sector == null)
                {
                    continue;
                }

                var multiplier = SectorCompetitionService.GetCachedRevenueMultiplier(job.Sector);
                var safeMultiplier = multiplier > 0f ? multiplier : 0.01f;

                var effectiveWeight = (int)System.Math.Ceiling(job.SelectionWeight * multiplier * multiplier);
                if (effectiveWeight < 1) effectiveWeight = 1;

                var effectiveAbandon = job.AbandonChance / safeMultiplier / safeMultiplier;
                var effectiveSell = job.AbandonChance * definition.SellDesireMultiplier / safeMultiplier / safeMultiplier;

                SharedBuilder.Append("\n  [");
                SharedBuilder.Append(job.Sector.DisplayName);
                SharedBuilder.Append("] Seçim Ağırlığı: ");
                SharedBuilder.Append(job.SelectionWeight);
                SharedBuilder.Append(" → ");
                SharedBuilder.Append(effectiveWeight);
                SharedBuilder.Append("  |  Bırakma: ");
                SharedBuilder.Append((effectiveAbandon * 100f).ToString("F1"));
                SharedBuilder.Append("%  |  Satma: ");
                SharedBuilder.Append((effectiveSell * 100f).ToString("F1"));
                SharedBuilder.Append("%");
            }

            var text = CreateInfoCard(SharedBuilder.ToString(), 48f + jobs.Count * 26f);
            text.supportRichText = true;
        }

        private void SwitchTab(int tab)
        {
            activeTab = tab;
            RefreshPage();
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

        private void BuildUi()
        {
            CreateOpenButton();
            CreatePanel();
            panelRoot.SetActive(false);
        }

        private void CreateOpenButton()
        {
            var button = CreateButton(rootCanvas.transform, "DebugOpenButton", "Debug Panel");
            var buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 1f);
            buttonRect.anchorMax = new Vector2(0f, 1f);
            buttonRect.pivot = new Vector2(0f, 1f);
            buttonRect.anchoredPosition = new Vector2(1280f, -80f);
            buttonRect.sizeDelta = new Vector2(200f, 44f);
            button.onClick.AddListener(OpenPanel);
        }

        private void CreatePanel()
        {
            panelRoot = CreateUiObject("DebugPanel", rootCanvas.transform);
            var panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(0f, -10f);
            panelRect.sizeDelta = panelSize;

            panelRoot.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.98f);

            var headerRoot = CreateUiObject("Header", panelRoot.transform);
            var headerRect = headerRoot.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0f, 70f);
            headerRoot.AddComponent<Image>().color = new Color(0.14f, 0.18f, 0.26f, 1f);

            pageTitleText = CreateText(headerRoot.transform, "Debug Panel", 26, TextAnchor.MiddleLeft);
            RuntimePanelUiUtility.StretchToParent(pageTitleText.rectTransform, 18f, 8f, 180f, 8f);

            var closeButton = CreateButton(headerRoot.transform, "CloseButton", "×");
            var closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-14f, 0f);
            closeRect.sizeDelta = new Vector2(50f, 40f);
            closeButton.onClick.AddListener(ClosePanel);

            var tabBar = CreateUiObject("TabBar", panelRoot.transform);
            var tabBarRect = tabBar.GetComponent<RectTransform>();
            tabBarRect.anchorMin = new Vector2(0f, 1f);
            tabBarRect.anchorMax = new Vector2(1f, 1f);
            tabBarRect.pivot = new Vector2(0.5f, 1f);
            tabBarRect.anchoredPosition = new Vector2(0f, -70f);
            tabBarRect.sizeDelta = new Vector2(0f, 44f);
            tabBar.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.2f, 1f);

            var tabLayout = tabBar.AddComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 6f;
            tabLayout.childControlWidth = true;
            tabLayout.childControlHeight = true;
            tabLayout.childForceExpandWidth = true;
            tabLayout.childForceExpandHeight = true;
            tabLayout.padding = new RectOffset(8, 8, 4, 4);

            var sectorTabButton = CreateButton(tabBar.transform, "SectorTab", "Sektörler");
            sectorTabButton.onClick.AddListener(() => SwitchTab(0));

            var rivalTabButton = CreateButton(tabBar.transform, "RivalTab", "Şirketler");
            rivalTabButton.onClick.AddListener(() => SwitchTab(1));

            var scrollRoot = CreateUiObject("ScrollRoot", panelRoot.transform);
            var scrollRectTransform = scrollRoot.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0f, 0f);
            scrollRectTransform.anchorMax = new Vector2(1f, 1f);
            scrollRectTransform.offsetMin = new Vector2(16f, 16f);
            scrollRectTransform.offsetMax = new Vector2(-16f, -130f);
            scrollRoot.AddComponent<Image>().color = new Color(0.11f, 0.13f, 0.17f, 0.92f);

            var scrollRect = scrollRoot.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 25f;

            var viewport = CreateUiObject("Viewport", scrollRoot.transform);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(8f, 8f);
            viewportRect.offsetMax = new Vector2(-8f, -8f);
            viewport.AddComponent<RectMask2D>();
            viewport.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);

            var content = CreateUiObject("Content", viewport.transform);
            contentRoot = content.GetComponent<RectTransform>();
            contentRoot.anchorMin = new Vector2(0f, 1f);
            contentRoot.anchorMax = new Vector2(1f, 1f);
            contentRoot.pivot = new Vector2(0.5f, 1f);
            contentRoot.anchoredPosition = Vector2.zero;
            contentRoot.sizeDelta = new Vector2(0f, 0f);

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRoot;
        }

        private Text CreateInfoCard(string message, float height = 58f)
        {
            return RuntimePanelUiUtility.CreateInfoCard(contentRoot, defaultFont, message, height);
        }

        private Button CreateButton(Transform parent, string objectName, string label)
        {
            return RuntimePanelUiUtility.CreateButton(parent, defaultFont, objectName, label);
        }

        private Text CreateText(Transform parent, string value, int fontSize, TextAnchor anchor)
        {
            return RuntimePanelUiUtility.CreateText(parent, defaultFont, value, fontSize, anchor);
        }

        private GameObject CreateUiObject(string objectName, Transform parent)
        {
            return RuntimePanelUiUtility.CreateUiObject(objectName, parent);
        }

        private Font LoadDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
