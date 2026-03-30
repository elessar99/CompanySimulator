using System;
using System.Collections.Generic;
using System.Reflection;
using CompanySimulator.Features.Accounting.Runtime.Components;
using CompanySimulator.Features.Agents.Runtime.Components;
using CompanySimulator.Features.Banking.Runtime.Components;
using CompanySimulator.Features.Employees.Runtime.Components;
using CompanySimulator.Features.Employees.Runtime.Models;
using CompanySimulator.Features.Finance.Runtime.Components;
using CompanySimulator.Features.Finance.Runtime.Definitions;
using CompanySimulator.Features.Finance.Runtime.Models;
using CompanySimulator.Features.Investments.Runtime.Definitions;
using CompanySimulator.Presentation.UI.Runtime.Common;
using CompanySimulator.Features.Projects.Runtime.Definitions;
using CompanySimulator.Features.Sectors.Runtime.Definitions;
using CompanySimulator.Features.Sectors.Runtime.Components;
using CompanySimulator.Features.Sectors.Runtime.Models;
using CompanySimulator.Features.Sectors.Runtime.Services;
using CompanySimulator.Shared.Runtime.Definitions;
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
        [SerializeField] private EmployeeManager employeeManager;
        [SerializeField] private CompanyAccountingManager companyAccountingManager;
        [SerializeField] private EmployeePanelUI employeePanelUI;
        [SerializeField] private AccountingPanelUI accountingPanelUI;
        [SerializeField] private BankPanelUI bankPanelUI;
        [SerializeField] private FinanceOverviewPanelUI financeOverviewPanelUI;
        [SerializeField] private RivalCompanyPanelUI rivalCompanyPanelUI;
        [SerializeField] private DebugPanelUI debugPanelUI;
        [SerializeField] private SecurityPanelUI securityPanelUI;
        [SerializeField] private AgentManager agentManager;
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private Vector2 panelSize = new Vector2(980f, 720f);

        private static readonly Color ColBg = new Color(0.035f, 0.067f, 0.122f, 0.985f);
        private static readonly Color ColPanel = new Color(0.063f, 0.098f, 0.169f, 1f);
        private static readonly Color ColSurface = new Color(0.082f, 0.125f, 0.204f, 1f);
        private static readonly Color ColSurfaceAlt = new Color(0.047f, 0.078f, 0.141f, 1f);
        private static readonly Color ColText = new Color(0.933f, 0.957f, 1f, 1f);
        private static readonly Color ColMuted = new Color(0.561f, 0.639f, 0.784f, 1f);
        private static readonly Color ColBlue = new Color(0.353f, 0.627f, 1f, 1f);
        private static readonly Color ColCyan = new Color(0.302f, 0.886f, 0.816f, 1f);
        private static readonly Color ColGold = new Color(0.961f, 0.769f, 0.365f, 1f);
        private static readonly Color ColGreen = new Color(0.263f, 0.839f, 0.561f, 1f);
        private static readonly Color ColRed = new Color(1f, 0.42f, 0.506f, 1f);
        private static readonly Color ColPurple = new Color(0.62f, 0.46f, 1f, 1f);

        private readonly Dictionary<InvestmentTypeDefinition, int> draftBudgetCache = new Dictionary<InvestmentTypeDefinition, int>(8);
        private readonly Dictionary<string, EmployeeRuntimeData> draftEmployeeSelections = new Dictionary<string, EmployeeRuntimeData>(16);

        private Font defaultFont;
        private Sprite roundedSprite;
        private GameObject panelRoot;
        private RectTransform contentRoot;
        private Text balanceText;
        private Text dayText;
        private Text pageTitleText;
        private Text draftResultText;
        private Button backButton;
        private Button agentDismissButton;
        private SectorRuntimeData selectedSector;
        private ProjectExecutionDefinition selectedProjectTemplate;
        private ActiveProjectRuntimeEntry selectedActiveProject;
        private InvestmentTypeDefinition selectedPropertyInvestment;
        private Transform sectorListGridParent;
        private Transform activeProjectGridParent;
        private string expandedEmployeeSlotId;
        private PageState currentPage;

        private void Awake()
        {
            economyManager ??= FindObjectOfType<EconomyManager>();
            sectorManager ??= FindObjectOfType<SectorManager>();
            employeeManager ??= FindObjectOfType<EmployeeManager>();
            companyAccountingManager ??= FindObjectOfType<CompanyAccountingManager>();
            if (companyAccountingManager == null)
            {
                companyAccountingManager = new GameObject("CompanyAccountingManager", typeof(CompanyAccountingManager)).GetComponent<CompanyAccountingManager>();
            }

            employeePanelUI ??= FindObjectOfType<EmployeePanelUI>();
            accountingPanelUI ??= FindObjectOfType<AccountingPanelUI>();
            if (accountingPanelUI == null)
            {
                accountingPanelUI = new GameObject("AccountingPanelUI", typeof(AccountingPanelUI)).GetComponent<AccountingPanelUI>();
            }

            bankPanelUI ??= FindObjectOfType<BankPanelUI>();
            if (bankPanelUI == null)
            {
                bankPanelUI = new GameObject("BankPanelUI", typeof(BankPanelUI)).GetComponent<BankPanelUI>();
            }

            financeOverviewPanelUI ??= FindObjectOfType<FinanceOverviewPanelUI>();
            if (financeOverviewPanelUI == null)
            {
                financeOverviewPanelUI = new GameObject("FinanceOverviewPanelUI", typeof(FinanceOverviewPanelUI)).GetComponent<FinanceOverviewPanelUI>();
            }

            rivalCompanyPanelUI ??= FindObjectOfType<RivalCompanyPanelUI>();
            debugPanelUI ??= FindObjectOfType<DebugPanelUI>();
            securityPanelUI ??= FindObjectOfType<SecurityPanelUI>();
            agentManager ??= FindObjectOfType<AgentManager>();

            EnsureCanvas();
            EnsureEventSystem();
            defaultFont = LoadDefaultFont();
            roundedSprite = LoadRoundedSprite();
            BuildStaticUi();
        }

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

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
            if (employeePanelUI != null && employeePanelUI.IsOpen)
            {
                employeePanelUI.ClosePanel();
            }

            if (accountingPanelUI != null && accountingPanelUI.IsOpen)
            {
                accountingPanelUI.ClosePanel();
            }

            if (bankPanelUI != null && bankPanelUI.IsOpen)
            {
                bankPanelUI.ClosePanel();
            }

            if (financeOverviewPanelUI != null && financeOverviewPanelUI.IsOpen)
            {
                financeOverviewPanelUI.ClosePanel();
            }

            if (rivalCompanyPanelUI != null && rivalCompanyPanelUI.IsOpen)
            {
                rivalCompanyPanelUI.ClosePanel();
            }

            if (debugPanelUI != null && debugPanelUI.IsOpen)
            {
                debugPanelUI.ClosePanel();
            }

            if (securityPanelUI != null && securityPanelUI.IsOpen)
            {
                securityPanelUI.ClosePanel();
            }

            panelRoot.SetActive(true);
            NavigateToSectorList();
        }

        public void ClosePanel()
        {
            NavigateToSectorList();
            panelRoot.SetActive(false);
        }

        private void GoBack()
        {
            switch (currentPage)
            {
                case PageState.ActiveProjectEdit:
                    ShowSectorDetails(selectedSector);
                    return;
                case PageState.NewJob:
                    ShowSectorDetails(selectedSector);
                    return;
                case PageState.SectorDetails:
                    NavigateToSectorList();
                    return;
                default:
                    return;
            }
        }

        private void NavigateToSectorList()
        {
            selectedSector = null;
            selectedProjectTemplate = null;
            selectedActiveProject = null;
            selectedPropertyInvestment = null;
            expandedEmployeeSlotId = null;
            draftEmployeeSelections.Clear();
            draftBudgetCache.Clear();
            currentPage = PageState.SectorList;
            UpdateHeaderButtons();
            ShowSectorList();
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

            if (employeeManager != null && !employeeManager.IsInitialized)
            {
                employeeManager.Initialize();
            }

            switch (currentPage)
            {
                case PageState.SectorDetails:
                    ShowSectorDetails(selectedSector);
                    break;
                case PageState.ActiveProjectEdit:
                    ShowActiveProjectEditor(selectedSector, selectedActiveProject);
                    break;
                case PageState.NewJob:
                    ShowNewJobPage(selectedSector);
                    break;
                default:
                    ShowSectorList();
                    break;
            }
        }

        private void SubscribeEvents()
        {
            if (economyManager != null)
            {
                economyManager.BalanceChanged -= HandleBalanceChanged;
                economyManager.BalanceChanged += HandleBalanceChanged;
                economyManager.DayAdvanced -= HandleDayAdvanced;
                economyManager.DayAdvanced += HandleDayAdvanced;
            }

            if (sectorManager != null)
            {
                sectorManager.DataChanged -= HandleSectorDataChanged;
                sectorManager.DataChanged += HandleSectorDataChanged;
            }

            if (employeeManager != null)
            {
                employeeManager.DataChanged -= HandleEmployeeDataChanged;
                employeeManager.DataChanged += HandleEmployeeDataChanged;
            }

            if (agentManager != null)
            {
                agentManager.DataChanged -= HandleAgentDataChanged;
                agentManager.DataChanged += HandleAgentDataChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            if (economyManager != null)
            {
                economyManager.BalanceChanged -= HandleBalanceChanged;
                economyManager.DayAdvanced -= HandleDayAdvanced;
            }

            if (sectorManager != null)
            {
                sectorManager.DataChanged -= HandleSectorDataChanged;
            }

            if (employeeManager != null)
            {
                employeeManager.DataChanged -= HandleEmployeeDataChanged;
            }

            if (agentManager != null)
            {
                agentManager.DataChanged -= HandleAgentDataChanged;
            }
        }

        private void HandleBalanceChanged(Money _)
        {
            RefreshBalanceText();
        }

        private void HandleDayAdvanced(int _)
        {
            RefreshDayText();
            RefreshAgentButtons();
            if (currentPage == PageState.NewJob || currentPage == PageState.SectorDetails)
            {
                RefreshAll();
            }
        }

        private void HandleSectorDataChanged()
        {
            RefreshAll();
        }

        private void HandleEmployeeDataChanged()
        {
            if (currentPage == PageState.NewJob || currentPage == PageState.ActiveProjectEdit)
            {
                RefreshAll();
            }
        }

        private void HandleAgentDataChanged()
        {
            RefreshAgentButtons();
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
            CreateDayWidget();
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
            balanceRect.sizeDelta = new Vector2(320f, 48f);

            ApplyRoundedImage(balanceRoot, ColPanel);

            balanceText = CreateText(balanceRoot.transform, "Para: 0", 22, TextAnchor.MiddleLeft);
            balanceText.color = ColText;
            StretchToParent(balanceText.rectTransform, 14f, 6f, 14f, 6f);
        }

        private void CreateOpenButton()
        {
            var button = CreateStyledButton(rootCanvas.transform, "SectorsOpenButton", "Sektörler", ColSurface, Blend(ColSurface, ColBlue, 0.25f), Darken(ColSurface, 0.16f), ColText, TextAnchor.MiddleCenter);
            var buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 1f);
            buttonRect.anchorMax = new Vector2(0f, 1f);
            buttonRect.pivot = new Vector2(0f, 1f);
            buttonRect.anchoredPosition = new Vector2(20f, -80f);
            buttonRect.sizeDelta = new Vector2(180f, 44f);
            button.onClick.AddListener(OpenPanel);
        }

        private void CreateDayWidget()
        {
            var dayRoot = CreateUiObject("DayBar", rootCanvas.transform);
            var dayRect = dayRoot.GetComponent<RectTransform>();
            dayRect.anchorMin = new Vector2(1f, 1f);
            dayRect.anchorMax = new Vector2(1f, 1f);
            dayRect.pivot = new Vector2(1f, 1f);
            dayRect.anchoredPosition = new Vector2(-220f, -20f);
            dayRect.sizeDelta = new Vector2(180f, 48f);
            ApplyRoundedImage(dayRoot, ColPanel);

            dayText = CreateText(dayRoot.transform, "Gün: 1", 22, TextAnchor.MiddleCenter);
            dayText.color = ColText;
            StretchToParent(dayText.rectTransform, 12f, 6f, 12f, 6f);

            var nextDayButton = CreateStyledButton(rootCanvas.transform, "NextDayButton", "Sonraki Gün", ColBlue, Blend(ColBlue, ColCyan, 0.25f), Darken(ColBlue, 0.2f), ColText, TextAnchor.MiddleCenter);
            var buttonRect = nextDayButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(1f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(1f, 1f);
            buttonRect.anchoredPosition = new Vector2(-20f, -20f);
            buttonRect.sizeDelta = new Vector2(180f, 44f);
            nextDayButton.onClick.AddListener(() =>
            {
                if (economyManager != null)
                {
                    economyManager.AdvanceDay();
                }
            });

            agentDismissButton = CreateStyledButton(rootCanvas.transform, "AgentDismissButton", "Ajanlarý Kov", new Color(ColRed.r, ColRed.g, ColRed.b, 0.16f), new Color(ColRed.r, ColRed.g, ColRed.b, 0.26f), new Color(ColRed.r, ColRed.g, ColRed.b, 0.34f), ColRed, TextAnchor.MiddleCenter);
            var dismissRect = agentDismissButton.GetComponent<RectTransform>();
            dismissRect.anchorMin = new Vector2(1f, 1f);
            dismissRect.anchorMax = new Vector2(1f, 1f);
            dismissRect.pivot = new Vector2(1f, 1f);
            dismissRect.anchoredPosition = new Vector2(-20f, -70f);
            dismissRect.sizeDelta = new Vector2(180f, 44f);
            agentDismissButton.onClick.AddListener(() =>
            {
                if (agentManager != null)
                {
                    agentManager.DismissDetectedAgents();
                    RefreshAgentButtons();
                }
            });

            RefreshAgentButtons();
            RefreshDayText();
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
            ApplyRoundedImage(panelRoot, ColBg);
            EnsureRoundedMask(panelRoot);

            var headerRoot = CreateUiObject("Header", panelRoot.transform);
            var headerRect = headerRoot.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0f, 82f);
            ApplyRoundedImage(headerRoot, ColPanel);
            EnsureRoundedMask(headerRoot);

            var badge = CreateRoundedBlock(headerRoot.transform, "HeaderBadge", new Vector2(48f, 48f), new Color(ColCyan.r, ColCyan.g, ColCyan.b, 0.18f));
            var badgeRect = badge.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0f, 0.5f);
            badgeRect.anchorMax = new Vector2(0f, 0.5f);
            badgeRect.pivot = new Vector2(0f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(18f, 0f);
            var badgeText = CreateText(badge.transform, "SEC", 16, TextAnchor.MiddleCenter);
            badgeText.color = ColCyan;
            badgeText.fontStyle = FontStyle.Bold;
            StretchToParent(badgeText.rectTransform, 0f, 0f, 0f, 0f);

            pageTitleText = CreateText(headerRoot.transform, "Sektörler", 28, TextAnchor.MiddleLeft);
            pageTitleText.color = ColText;
            pageTitleText.fontStyle = FontStyle.Bold;
            pageTitleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            pageTitleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            pageTitleText.rectTransform.offsetMin = new Vector2(86f, -50f);
            pageTitleText.rectTransform.offsetMax = new Vector2(-140f, -14f);

            backButton = CreateStyledButton(headerRoot.transform, "BackButton", "?", ColSurfaceAlt, Blend(ColSurfaceAlt, ColBlue, 0.18f), Darken(ColSurfaceAlt, 0.15f), ColText, TextAnchor.MiddleCenter);
            var backRect = backButton.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(1f, 0.5f);
            backRect.anchorMax = new Vector2(1f, 0.5f);
            backRect.pivot = new Vector2(1f, 0.5f);
            backRect.anchoredPosition = new Vector2(-72f, 0f);
            backRect.sizeDelta = new Vector2(50f, 40f);
            backButton.onClick.AddListener(GoBack);

            var closeButton = CreateStyledButton(headerRoot.transform, "CloseButton", "×", new Color(ColRed.r, ColRed.g, ColRed.b, 0.16f), new Color(ColRed.r, ColRed.g, ColRed.b, 0.28f), new Color(ColRed.r, ColRed.g, ColRed.b, 0.4f), ColRed, TextAnchor.MiddleCenter);
            var closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-14f, 0f);
            closeRect.sizeDelta = new Vector2(50f, 40f);
            closeButton.onClick.AddListener(ClosePanel);

            var scrollRoot = CreateUiObject("ScrollRoot", panelRoot.transform);
            var scrollRectTransform = scrollRoot.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0f, 0f);
            scrollRectTransform.anchorMax = new Vector2(1f, 1f);
            scrollRectTransform.offsetMin = new Vector2(16f, 16f);
            scrollRectTransform.offsetMax = new Vector2(-16f, -86f);
            ApplyRoundedImage(scrollRoot, new Color(ColPanel.r, ColPanel.g, ColPanel.b, 0.72f));
            EnsureRoundedMask(scrollRoot);

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
            layout.spacing = 12f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRoot;
            UpdateHeaderButtons();
        }

        private void ShowSectorList()
        {
            currentPage = PageState.SectorList;
            UpdateHeaderButtons();
            pageTitleText.text = "Sektörler";
            draftResultText = null;
            sectorListGridParent = null;
            ClearChildren(contentRoot);

            SectorListPage.Render(sectorManager, message => CreateInfoCard(message), CreateSectorButton);
        }

        private void ShowSectorDetails(SectorRuntimeData sectorData)
        {
            if (contentRoot == null)
            {
                return;
            }

            if (sectorData == null)
            {
                NavigateToSectorList();
                return;
            }

            currentPage = PageState.SectorDetails;
            selectedSector = sectorData;
            UpdateHeaderButtons();
            pageTitleText.text = sectorData.Sector.DisplayName;
            draftResultText = null;
            activeProjectGridParent = null;
            ClearChildren(contentRoot);

            SectorDetailsPage.Render(sectorData, economyManager, message => CreateInfoCard(message), (message, height) => CreateInfoCard(message, height), CreateSectionTitle, CreateActiveProjectCards);

            var newJobButton = CreateStyledButton(contentRoot, "NewJobButton", "+ Yeni Ýþ", ColBlue, Blend(ColBlue, ColCyan, 0.28f), Darken(ColBlue, 0.22f), ColText, TextAnchor.MiddleCenter);
            var newJobLayout = newJobButton.gameObject.AddComponent<LayoutElement>();
            newJobLayout.preferredHeight = 52f;
            newJobLayout.minHeight = 52f;
            newJobButton.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 52f);
            newJobButton.onClick.AddListener(() => ShowNewJobPage(sectorData));
        }

        private void ShowNewJobPage(SectorRuntimeData sectorData)
        {
            if (contentRoot == null)
            {
                return;
            }

            if (sectorData == null)
            {
                NavigateToSectorList();
                return;
            }

            currentPage = PageState.NewJob;
            selectedSector = sectorData;
            selectedActiveProject = null;
            UpdateHeaderButtons();
            pageTitleText.text = sectorData.Sector.DisplayName + " / Yeni Ýþ";
            ClearChildren(contentRoot);

            if (!IsProjectUsableForSector(sectorData, selectedProjectTemplate))
            {
                selectedProjectTemplate = sectorData.AvailableProjects.Count > 0
                    ? sectorData.AvailableProjects[0]
                    : CreateTransientProjectTemplate(sectorData.Sector);
                selectedPropertyInvestment = null;
                expandedEmployeeSlotId = null;
                draftEmployeeSelections.Clear();
            }

            if (selectedProjectTemplate == null)
            {
                CreateInfoCard("Bu sektörde kullanýlabilecek hazýr iþ þablonu bulunmuyor.");
                return;
            }

            CleanupDraftState(selectedProjectTemplate);

            if (companyAccountingManager != null)
            {
                var capacitySnapshot = companyAccountingManager.GetCurrentCycleSnapshot();
                CreateInfoCard($"Ýþ Kapasitesi: {capacitySnapshot.ActiveProjectCount} / {capacitySnapshot.MaxActiveProjectCount}", 58f);
                if (!companyAccountingManager.CanCreateAdditionalProject(out var capacityValidationMessage))
                {
                    CreateInfoCard(capacityValidationMessage, 72f);
                }
            }

            SectorNewJobPage.Render(sectorData, selectedProjectTemplate, message => CreateInfoCard(message), (message, height) => CreateInfoCard(message, height), CreateSectionTitle, CreateProjectTemplateSelector, CreateEmployeeRequirementCards, CreateInvestmentEditors);

            CreateSectionTitle("Önizleme");
            var previewButton = CreateStyledButton(contentRoot, "PreviewButton", "Önizleme Yap", ColSurface, Blend(ColSurface, ColBlue, 0.16f), Darken(ColSurface, 0.12f), ColText, TextAnchor.MiddleCenter);
            var previewLayout = previewButton.gameObject.AddComponent<LayoutElement>();
            previewLayout.preferredHeight = 52f;
            previewLayout.minHeight = 52f;
            previewButton.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 52f);
            previewButton.onClick.AddListener(PreviewDraft);

            draftResultText = CreateInfoCard("Çalýþan seçip yatýrým tutarlarýný girdikten sonra önizleme yapabilirsin.", 148f);

            var startButton = CreateStyledButton(contentRoot, "StartButton", "Ýþi Baþlat", ColBlue, Blend(ColBlue, ColCyan, 0.28f), Darken(ColBlue, 0.22f), ColText, TextAnchor.MiddleCenter);
            var startLayout = startButton.gameObject.AddComponent<LayoutElement>();
            startLayout.preferredHeight = 52f;
            startLayout.minHeight = 52f;
            startButton.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 52f);
            startButton.onClick.AddListener(StartDraft);
        }

        private void ShowActiveProjectEditor(SectorRuntimeData sectorData, ActiveProjectRuntimeEntry activeProject)
        {
            if (contentRoot == null)
            {
                return;
            }

            if (sectorData == null || activeProject == null)
            {
                ShowSectorDetails(sectorData);
                return;
            }

            var isSameProject = selectedActiveProject == activeProject && currentPage == PageState.ActiveProjectEdit;
            currentPage = PageState.ActiveProjectEdit;
            selectedSector = sectorData;
            selectedActiveProject = activeProject;
            selectedProjectTemplate = activeProject.SourceDefinition;
            UpdateHeaderButtons();
            pageTitleText.text = activeProject.DisplayName + " / Düzenle";
            ClearChildren(contentRoot);

            if (!isSameProject)
            {
                LoadDraftFromActiveProject(activeProject);
            }

            SectorActiveProjectEditPage.Render(activeProject, economyManager, message => CreateInfoCard(message), (message, height) => CreateInfoCard(message, height), CreateSectionTitle, CreateEmployeeRequirementCards, CreateInvestmentEditors);

            CreateSectionTitle("Önizleme");
            var previewButton = CreateStyledButton(contentRoot, "PreviewActiveButton", "Deðiþikliði Önizle", ColSurface, Blend(ColSurface, ColBlue, 0.16f), Darken(ColSurface, 0.12f), ColText, TextAnchor.MiddleCenter);
            var previewLayout = previewButton.gameObject.AddComponent<LayoutElement>();
            previewLayout.preferredHeight = 52f;
            previewLayout.minHeight = 52f;
            previewButton.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 52f);
            previewButton.onClick.AddListener(PreviewDraft);

            draftResultText = CreateInfoCard("Çalýþan veya bütçe artýþýný yaptýktan sonra önizleme alabilirsin.", 132f);

            var updateButton = CreateStyledButton(contentRoot, "UpdateActiveJobButton", "Deðiþiklikleri Uygula", ColBlue, Blend(ColBlue, ColCyan, 0.28f), Darken(ColBlue, 0.22f), ColText, TextAnchor.MiddleCenter);
            var updateLayout = updateButton.gameObject.AddComponent<LayoutElement>();
            updateLayout.preferredHeight = 52f;
            updateLayout.minHeight = 52f;
            updateButton.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 52f);
            updateButton.onClick.AddListener(() =>
            {
                if (!TryBuildDraftRequest(out var request, out var validationMessage))
                {
                    if (draftResultText != null)
                    {
                        draftResultText.text = validationMessage;
                    }

                    return;
                }

                ApplyActiveProjectChanges(request);
            });
        }

        private void RefreshDraftPage()
        {
            if (currentPage == PageState.ActiveProjectEdit && selectedActiveProject != null)
            {
                ShowActiveProjectEditor(selectedSector, selectedActiveProject);
                return;
            }

            ShowNewJobPage(selectedSector);
        }

        private void CreateSectorButton(SectorRuntimeData sectorData)
        {
            var gridParent = EnsureSectorListGridHost();
            var accent = GetSectorAccent(sectorData.Sector);
            var cardObject = CreateUiObject($"Sector_{sectorData.Sector.Id}", gridParent);
            var cardRect = cardObject.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(400f, 186f);
            ApplyRoundedImage(cardObject, ColPanel);
            AddHoverEffect(cardObject, ColPanel, Blend(ColPanel, accent, 0.18f));
            CreateAccentBar(cardObject.transform, accent);

            var cardLayout = cardObject.AddComponent<LayoutElement>();
            cardLayout.preferredWidth = 400f;
            cardLayout.minWidth = 400f;
            cardLayout.preferredHeight = 186f;
            cardLayout.minHeight = 186f;

            var button = cardObject.AddComponent<Button>();
            button.targetGraphic = cardObject.GetComponent<Image>();
            button.colors = CreateButtonColors(ColPanel, Blend(ColPanel, accent, 0.18f), Darken(ColPanel, 0.12f));

            var content = CreateStretchContainer(cardObject.transform, "Content", 14f, 12f, 14f, 12f);
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 0, 0, 0);
            contentLayout.spacing = 8f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var topRow = CreateUiObject("TopRow", content.transform);
            topRow.AddComponent<LayoutElement>().preferredHeight = 30f;
            var topLayout = topRow.AddComponent<HorizontalLayoutGroup>();
            topLayout.spacing = 8f;
            topLayout.childControlWidth = false;
            topLayout.childControlHeight = true;
            topLayout.childForceExpandWidth = false;
            topLayout.childForceExpandHeight = false;
            topLayout.childAlignment = TextAnchor.MiddleLeft;

            CreateTag(topRow.transform, $"Aktif {sectorData.ActiveProjectCount}", new Color(accent.r, accent.g, accent.b, 0.18f), accent, 13);
            CreateTag(topRow.transform, $"{sectorData.Sector.ProfitPayoutIntervalDays}g Döngü", new Color(ColBlue.r, ColBlue.g, ColBlue.b, 0.16f), ColBlue, 13);

            var title = CreateText(content.transform, sectorData.Sector.DisplayName, 22, TextAnchor.MiddleLeft);
            title.color = ColText;
            title.fontStyle = FontStyle.Bold;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

            var subtitle = CreateText(content.transform, "Sektör detaylarýný aç ve aktif iþleri yönet.", 14, TextAnchor.MiddleLeft);
            subtitle.color = ColMuted;
            subtitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

            CreateFlexibleSpacer(content.transform);

            var statRow = CreateUiObject("StatsRow", content.transform);
            statRow.AddComponent<LayoutElement>().preferredHeight = 46f;
            var statGrid = statRow.AddComponent<GridLayoutGroup>();
            statGrid.cellSize = new Vector2(174f, 46f);
            statGrid.spacing = new Vector2(8f, 0f);
            statGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            statGrid.constraintCount = 2;
            statGrid.childAlignment = TextAnchor.MiddleCenter;

            CreateMiniStat(statRow.transform, sectorData.ActiveProjectCount.ToString(), "Aktif Ýþ");
            CreateMiniStat(statRow.transform, sectorData.Sector.ProfitPayoutIntervalDays + "g", "Gelir Döngüsü");
            button.onClick.AddListener(() => ShowSectorDetails(sectorData));
        }

        private void CreateActiveProjectCards(SectorRuntimeData sectorData)
        {
            activeProjectGridParent = null;
            SectorDetailsPage.RenderActiveProjects(sectorData, economyManager, CreateActiveProjectCard, (message, height) => CreateInfoCard(message, height));
        }

        private void CreateActiveProjectCard(ActiveProjectRuntimeEntry activeProject)
        {
            var employeeNames = activeProject.AssignedEmployeeNames.Count > 0
                ? string.Join(", ", activeProject.AssignedEmployeeNames)
                : "Atama bilgisi yok";
            var remainingDays = economyManager != null ? activeProject.DaysUntilNextPayout(economyManager.CurrentDay) : 0;
            var adjustedRevenue = activeProject.CompetitionAdjustedCycleRevenue;
            var adjustedProfit = activeProject.CompetitionAdjustedCycleProfit;
            var accent = GetSectorAccent(activeProject.Sector);
            var card = CreateSurface(EnsureActiveProjectGridHost(), $"ActiveProject_{activeProject.DisplayName}", 228f, ColPanel);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(400f, 228f);
            var cardLayout = card.GetComponent<LayoutElement>();
            cardLayout.preferredWidth = 400f;
            cardLayout.minWidth = 400f;
            AddHoverEffect(card, ColPanel, Blend(ColPanel, accent, 0.18f));
            CreateAccentBar(card.transform, accent);

            var content = CreateStretchContainer(card.transform, "Content", 12f, 12f, 12f, 12f);
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 0, 0, 0);
            contentLayout.spacing = 8f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var title = CreateText(content.transform, activeProject.DisplayName, 22, TextAnchor.MiddleLeft);
            title.color = ColText;
            title.fontStyle = FontStyle.Bold;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

            var statRow = CreateUiObject("ProjectStats", content.transform);
            statRow.AddComponent<LayoutElement>().preferredHeight = 42f;
            var statGrid = statRow.AddComponent<GridLayoutGroup>();
            statGrid.cellSize = new Vector2(116f, 42f);
            statGrid.spacing = new Vector2(6f, 0f);
            statGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            statGrid.constraintCount = 3;
            statGrid.childAlignment = TextAnchor.MiddleCenter;

            CreateMiniStat(statRow.transform, remainingDays + "g", "Sonraki Gelir");
            CreateMiniStat(statRow.transform, adjustedRevenue.Amount.ToString("N0"), "Tahmini Gelir");
            CreateMiniStat(statRow.transform, adjustedProfit.Amount.ToString("N0"), "Tahmini Kâr");

            var employees = CreateText(content.transform, $"Çalýþanlar: {employeeNames}", 14, TextAnchor.MiddleLeft);
            employees.color = ColMuted;
            employees.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

            CreateFlexibleSpacer(content.transform);

            var buttonRow = CreateUiObject("ActionRow", content.transform);
            buttonRow.AddComponent<LayoutElement>().preferredHeight = 80f;
            var buttonLayout = buttonRow.AddComponent<VerticalLayoutGroup>();
            buttonLayout.spacing = 8f;
            buttonLayout.padding = new RectOffset(0, 0, 0, 0);
            buttonLayout.childControlWidth = true;
            buttonLayout.childControlHeight = true;
            buttonLayout.childForceExpandWidth = true;
            buttonLayout.childForceExpandHeight = false;

            var editButton = CreateStyledButton(buttonRow.transform, $"EditProject_{activeProject.DisplayName}", "Düzenle", ColBlue, Blend(ColBlue, ColCyan, 0.3f), Darken(ColBlue, 0.25f), ColText, TextAnchor.MiddleCenter);
            editButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
            editButton.onClick.AddListener(() => ShowActiveProjectEditor(selectedSector, activeProject));

            var saleMultiplier = activeProject.Sector != null ? activeProject.Sector.SaleRevenueMultiplier : 1f;
            var saleValue = Money.From(adjustedRevenue.Amount * saleMultiplier);
            var sellButton = CreateStyledButton(
                buttonRow.transform,
                $"SellProject_{activeProject.DisplayName}",
                $"Sat ({saleValue.Amount:N0})",
                new Color(ColRed.r, ColRed.g, ColRed.b, 0.16f),
                new Color(ColRed.r, ColRed.g, ColRed.b, 0.26f),
                new Color(ColRed.r, ColRed.g, ColRed.b, 0.34f),
                ColRed,
                TextAnchor.MiddleCenter);
            sellButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
            sellButton.onClick.AddListener(() =>
            {
                if (economyManager != null && economyManager.TrySellProject(activeProject, out _))
                {
                    ShowSectorDetails(selectedSector);
                }
            });
        }

        private bool ContainsProject(SectorRuntimeData sectorData, ProjectExecutionDefinition project)
        {
            var projects = sectorData.AvailableProjects;
            for (var i = 0; i < projects.Count; i++)
            {
                if (projects[i] == project)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsProjectUsableForSector(SectorRuntimeData sectorData, ProjectExecutionDefinition project)
        {
            if (sectorData == null || project == null)
            {
                return false;
            }

            if (ContainsProject(sectorData, project))
            {
                return true;
            }

            return sectorData.AvailableProjects.Count == 0 && project.ProjectType != null && project.ProjectType.Sector == sectorData.Sector;
        }

        private ProjectExecutionDefinition CreateTransientProjectTemplate(SectorDefinition sector)
        {
            if (sector == null)
            {
                return null;
            }

            var projectType = ScriptableObject.CreateInstance<ProjectTypeDefinition>();
            projectType.hideFlags = HideFlags.HideAndDontSave;
            projectType.name = sector.DisplayName + " Geçici Proje";
            SetDefinitionIdentity(projectType, "transient_project_" + sector.Id, sector.DisplayName + " Ýþi");
            SetField(projectType, "sector", sector);
            SetField(projectType, "baseRevenue", 250000);
            SetField(projectType, "fixedCost", 20000);
            SetField(projectType, "baseDurationDays", Mathf.Max(1, sector.ProfitPayoutIntervalDays));
            SetField(projectType, "baseSuccessScore", 1f);
            SetField(projectType, "demandMultiplier", 1f);
            SetField(projectType, "preferredRoles", ToArray(sector.SupportedRoles));
            SetField(projectType, "recommendedInvestments", ToArray(sector.AvailableInvestments));

            var execution = ScriptableObject.CreateInstance<ProjectExecutionDefinition>();
            execution.hideFlags = HideFlags.HideAndDontSave;
            execution.name = sector.DisplayName + " Geçici Ýþ";
            SetDefinitionIdentity(execution, "transient_execution_" + sector.Id, sector.DisplayName + " Ýþi");
            SetField(execution, "projectType", projectType);
            SetField(execution, "employeeAssignments", CreateTransientAssignments(sector));
            SetField(execution, "investmentAllocations", CreateTransientInvestments(sector));
            SetField(execution, "marketDemandMultiplier", 1f);
            SetField(execution, "competitorPressure", 0.1f);
            return execution;
        }

        private EmployeeAssignmentInput[] CreateTransientAssignments(SectorDefinition sector)
        {
            var roles = sector.SupportedRoles;
            var result = new EmployeeAssignmentInput[roles.Count];
            for (var i = 0; i < roles.Count; i++)
            {
                result[i] = new EmployeeAssignmentInput(roles[i], 1, 50f, 1f);
            }

            return result;
        }

        private InvestmentAllocationInput[] CreateTransientInvestments(SectorDefinition sector)
        {
            var investments = sector.AvailableInvestments;
            var result = new InvestmentAllocationInput[investments.Count];
            for (var i = 0; i < investments.Count; i++)
            {
                var investment = investments[i];
                result[i] = new InvestmentAllocationInput(investment, investment != null ? investment.RecommendedBudget : 0);
            }

            return result;
        }

        private void CleanupDraftState(ProjectExecutionDefinition project)
        {
            if (project == null)
            {
                draftEmployeeSelections.Clear();
                expandedEmployeeSlotId = null;
                selectedPropertyInvestment = null;
                return;
            }

            var validSlotIds = new HashSet<string>();
            var assignments = project.EmployeeAssignments;
            for (var i = 0; i < assignments.Count; i++)
            {
                var assignment = assignments[i];
                var slotCount = Mathf.Max(0, assignment.Count);
                for (var slotIndex = 0; slotIndex < slotCount; slotIndex++)
                {
                    validSlotIds.Add(BuildEmployeeSlotId(assignment.Role, i, slotIndex));
                }
            }

            var keysToRemove = new List<string>();
            foreach (var pair in draftEmployeeSelections)
            {
                if (!validSlotIds.Contains(pair.Key))
                {
                    keysToRemove.Add(pair.Key);
                }
            }

            for (var i = 0; i < keysToRemove.Count; i++)
            {
                draftEmployeeSelections.Remove(keysToRemove[i]);
            }

            var investments = GetEditableInvestments(project);
            if (selectedPropertyInvestment != null && !investments.Contains(selectedPropertyInvestment))
            {
                selectedPropertyInvestment = null;
            }
        }

        private void LoadDraftFromActiveProject(ActiveProjectRuntimeEntry activeProject)
        {
            draftEmployeeSelections.Clear();
            draftBudgetCache.Clear();
            expandedEmployeeSlotId = null;

            if (activeProject == null || selectedProjectTemplate == null)
            {
                return;
            }

            var assignedEmployees = activeProject.AssignedEmployees;
            var assignedSlotIds = activeProject.AssignedEmployeeSlotIds;
            if (assignedEmployees.Count == assignedSlotIds.Count && assignedSlotIds.Count > 0)
            {
                for (var i = 0; i < assignedSlotIds.Count; i++)
                {
                    var slotId = assignedSlotIds[i];
                    var employee = assignedEmployees[i];
                    if (string.IsNullOrWhiteSpace(slotId) || employee == null)
                    {
                        continue;
                    }

                    draftEmployeeSelections[slotId] = employee;
                }
            }
            else
            {
                var assignments = selectedProjectTemplate.EmployeeAssignments;
                var employeeIndex = 0;
                for (var assignmentIndex = 0; assignmentIndex < assignments.Count; assignmentIndex++)
                {
                    var assignment = assignments[assignmentIndex];
                    for (var slotIndex = 0; slotIndex < assignment.Count; slotIndex++)
                    {
                        if (employeeIndex >= assignedEmployees.Count)
                        {
                            break;
                        }

                        draftEmployeeSelections[BuildEmployeeSlotId(assignment.Role, assignmentIndex, slotIndex)] = assignedEmployees[employeeIndex];
                        employeeIndex++;
                    }
                }
            }

            var allocations = activeProject.CurrentInvestmentAllocations;
            for (var i = 0; i < allocations.Count; i++)
            {
                if (allocations[i].InvestmentType != null)
                {
                    draftBudgetCache[allocations[i].InvestmentType] = allocations[i].AllocatedBudgetAmount;
                }
            }

            selectedPropertyInvestment = null;
            for (var i = 0; i < allocations.Count; i++)
            {
                var investment = allocations[i].InvestmentType;
                if (investment == null)
                {
                    continue;
                }

                if (string.Equals(investment.Id, "kira", StringComparison.OrdinalIgnoreCase) || string.Equals(investment.Id, "satinalma", StringComparison.OrdinalIgnoreCase))
                {
                    selectedPropertyInvestment = investment;
                    break;
                }
            }
        }

        private void CreateProjectCard(SectorRuntimeData sectorData, ProjectExecutionDefinition project)
        {
            var activeCount = sectorData.GetActiveCount(project);
            var resultPreview = economyManager != null ? economyManager.PreviewProject(project) : default;
            var accent = GetSectorAccent(sectorData.Sector);
            var card = CreateSurface(contentRoot, $"Project_{project.Id}", 154f, ColPanel);
            AddHoverEffect(card, ColPanel, Blend(ColPanel, accent, 0.18f));
            CreateAccentBar(card.transform, accent);

            var button = card.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.colors = CreateButtonColors(ColPanel, Blend(ColPanel, accent, 0.18f), Darken(ColPanel, 0.12f));

            var content = CreateStretchContainer(card.transform, "Content", 12f, 12f, 12f, 12f);
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 0, 0, 0);
            contentLayout.spacing = 8f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var title = CreateText(content.transform, project.DisplayName, 20, TextAnchor.MiddleLeft);
            title.color = ColText;
            title.fontStyle = FontStyle.Bold;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;

            var subtitle = CreateText(content.transform, "Þablonu seç ve yeni iþ akýþýný baþlat.", 14, TextAnchor.MiddleLeft);
            subtitle.color = ColMuted;
            subtitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

            CreateFlexibleSpacer(content.transform);

            var stats = CreateUiObject("Stats", content.transform);
            stats.AddComponent<LayoutElement>().preferredHeight = 42f;
            var statGrid = stats.AddComponent<GridLayoutGroup>();
            statGrid.cellSize = new Vector2(174f, 42f);
            statGrid.spacing = new Vector2(8f, 0f);
            statGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            statGrid.constraintCount = 2;

            CreateMiniStat(stats.transform, activeCount.ToString(), "Aktif");
            CreateMiniStat(stats.transform, GetCycleProfit(resultPreview).Amount.ToString("N0"), "Döngü Kârý");
            button.onClick.AddListener(() =>
            {
                selectedProjectTemplate = project;
                selectedPropertyInvestment = null;
                expandedEmployeeSlotId = null;
                draftEmployeeSelections.Clear();
                ShowNewJobPage(sectorData);
            });
        }

        private void CreateProjectTemplateSelector(ProjectExecutionDefinition project)
        {
            var selectedText = project == selectedProjectTemplate ? "Seçili" : "Seç";
            var accent = project == selectedProjectTemplate ? ColCyan : ColBlue;
            var card = CreateSurface(contentRoot, $"Template_{project.Id}", 150f, ColPanel);
            AddHoverEffect(card, ColPanel, Blend(ColPanel, accent, 0.18f));
            CreateAccentBar(card.transform, accent);

            var button = card.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.colors = CreateButtonColors(ColPanel, Blend(ColPanel, accent, 0.18f), Darken(ColPanel, 0.12f));

            var content = CreateStretchContainer(card.transform, "Content", 12f, 12f, 12f, 12f);
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 0, 0, 0);
            contentLayout.spacing = 8f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var title = CreateText(content.transform, project.DisplayName, 20, TextAnchor.MiddleLeft);
            title.color = ColText;
            title.fontStyle = FontStyle.Bold;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;

            var subtitle = CreateText(content.transform, "Durumuna göre þablon deðiþtirip sayfayý yenile.", 14, TextAnchor.MiddleLeft);
            subtitle.color = ColMuted;
            subtitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

            CreateFlexibleSpacer(content.transform);
            CreateTag(content.transform, selectedText, new Color(accent.r, accent.g, accent.b, 0.18f), accent, 13);
            button.onClick.AddListener(() =>
            {
                selectedProjectTemplate = project;
                selectedPropertyInvestment = null;
                expandedEmployeeSlotId = null;
                draftEmployeeSelections.Clear();
                ShowNewJobPage(selectedSector);
            });
        }

        private void CreateEmployeeRequirementCards(ProjectExecutionDefinition project)
        {
            var assignments = project.EmployeeAssignments;
            if (assignments.Count == 0)
            {
                CreateInfoCard("Bu iþ için çalýþan tanýmý bulunmuyor.");
                return;
            }

            if (employeeManager == null)
            {
                CreateInfoCard("Çalýþan sistemi sahnede bulunamadý.");
                return;
            }

            for (var assignmentIndex = 0; assignmentIndex < assignments.Count; assignmentIndex++)
            {
                var assignment = assignments[assignmentIndex];
                var role = assignment.Role;
                if (role == null)
                {
                    continue;
                }

                var roleSection = CreateUiObject($"RoleSection_{role.Id}_{assignmentIndex}", contentRoot);
                var roleSectionLayout = roleSection.AddComponent<VerticalLayoutGroup>();
                roleSectionLayout.padding = new RectOffset(0, 0, assignmentIndex > 0 ? 10 : 0, 0);
                roleSectionLayout.spacing = 12f;
                roleSectionLayout.childControlWidth = true;
                roleSectionLayout.childControlHeight = true;
                roleSectionLayout.childForceExpandWidth = true;
                roleSectionLayout.childForceExpandHeight = false;

                var roleSectionFitter = roleSection.AddComponent<ContentSizeFitter>();
                roleSectionFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                roleSectionFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                for (var slotIndex = 0; slotIndex < assignment.Count; slotIndex++)
                {
                    CreateEmployeeSlotEditor(role, assignmentIndex, slotIndex, roleSection.transform);
                }
            }
        }

        private void CreateEmployeeSlotEditor(CompanySimulator.Features.Employees.Runtime.Definitions.EmployeeRoleDefinition role, int assignmentIndex, int slotIndex, Transform parent)
        {
            var slotId = BuildEmployeeSlotId(role, assignmentIndex, slotIndex);
            draftEmployeeSelections.TryGetValue(slotId, out var selectedEmployee);
            var accent = selectedEmployee != null ? ColGreen : ColBlue;
            var slotRow = CreateLeftAlignedCardRow(parent, 96f);
            var slotCard = CreateSurface(slotRow.transform, $"EmployeeSlot_{slotId}", 96f, ColPanel);
            var slotRect = slotCard.GetComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(400f, 96f);
            var slotLayout = slotCard.GetComponent<LayoutElement>();
            slotLayout.preferredWidth = 400f;
            slotLayout.minWidth = 400f;
            AddHoverEffect(slotCard, ColPanel, Blend(ColPanel, accent, 0.12f));
            CreateAccentBar(slotCard.transform, accent);

            var slotButton = slotCard.AddComponent<Button>();
            slotButton.targetGraphic = slotCard.GetComponent<Image>();
            slotButton.colors = CreateButtonColors(ColPanel, Blend(ColPanel, accent, 0.12f), Darken(ColPanel, 0.08f));

            var content = CreateStretchContainer(slotCard.transform, "Content", 16f, 12f, 16f, 12f);
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 0, 0, 0);
            contentLayout.spacing = 6f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var topRow = CreateUiObject("TopRow", content.transform);
            topRow.AddComponent<LayoutElement>().preferredHeight = 24f;
            var topRowLayout = topRow.AddComponent<HorizontalLayoutGroup>();
            topRowLayout.spacing = 8f;
            topRowLayout.childControlWidth = true;
            topRowLayout.childControlHeight = true;
            topRowLayout.childForceExpandWidth = true;
            topRowLayout.childForceExpandHeight = false;

            var title = CreateText(topRow.transform, $"{role.DisplayName} / Slot {slotIndex + 1}", 18, TextAnchor.MiddleLeft);
            title.color = ColText;
            title.fontStyle = FontStyle.Bold;

            CreateTag(topRow.transform, expandedEmployeeSlotId == slotId ? "Açýk" : "Kapalý", new Color(accent.r, accent.g, accent.b, 0.18f), accent, 13);

            var subtitle = CreateText(content.transform, selectedEmployee == null ? "Boþ çalýþan seç" : $"{selectedEmployee.DisplayName} | Kademe: {selectedEmployee.QualityTier} | x{selectedEmployee.IncomeMultiplier:0.0}", 14, TextAnchor.MiddleLeft);
            subtitle.color = ColMuted;
            subtitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

            var info = CreateText(content.transform, expandedEmployeeSlotId == slotId ? "Aday listesi görüntüleniyor" : "Adaylarý görmek için týkla", 13, TextAnchor.MiddleLeft);
            info.color = new Color(ColText.r, ColText.g, ColText.b, 0.75f);
            info.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

            slotButton.onClick.AddListener(() =>
            {
                expandedEmployeeSlotId = expandedEmployeeSlotId == slotId ? null : slotId;
                RefreshDraftPage();
            });

            if (expandedEmployeeSlotId != slotId)
            {
                return;
            }

            var availableEmployees = GetSelectableEmployees(role, slotId);
            if (availableEmployees.Count == 0)
            {
                CreateInfoCard(parent, $"{role.DisplayName} için boþta çalýþan bulunmuyor.", 60f);
            }
            else
            {
                const float candidateCardWidth = 400f;
                const float candidateCardHeight = 128f;
                const float candidateGridSpacing = 12f;
                var candidateColumnCount = CalculateGridColumnCount(candidateCardWidth, candidateGridSpacing);
                var candidateGridHeight = CalculateGridHeight(availableEmployees.Count, candidateColumnCount, candidateCardHeight, candidateGridSpacing);

                var candidateGridHost = CreateUiObject($"CandidateGrid_{slotId}", parent);
                var candidateGridLayout = candidateGridHost.AddComponent<LayoutElement>();
                candidateGridLayout.preferredHeight = candidateGridHeight;
                candidateGridLayout.minHeight = candidateGridHeight;

                var candidateGrid = candidateGridHost.AddComponent<GridLayoutGroup>();
                candidateGrid.cellSize = new Vector2(candidateCardWidth, candidateCardHeight);
                candidateGrid.spacing = new Vector2(candidateGridSpacing, candidateGridSpacing);
                candidateGrid.padding = new RectOffset(0, 0, 0, 0);
                candidateGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                candidateGrid.constraintCount = candidateColumnCount;
                candidateGrid.childAlignment = TextAnchor.UpperLeft;

                for (var i = 0; i < availableEmployees.Count; i++)
                {
                    CreateEmployeeCandidateButton(slotId, availableEmployees[i], candidateGridHost.transform);
                }
            }

            if (selectedEmployee != null)
            {
                var clearRow = CreateLeftAlignedCardRow(parent, 44f);
                var clearButton = CreateStyledButton(clearRow.transform, $"Clear_{slotId}", "Atamayý Temizle", new Color(ColRed.r, ColRed.g, ColRed.b, 0.16f), new Color(ColRed.r, ColRed.g, ColRed.b, 0.26f), new Color(ColRed.r, ColRed.g, ColRed.b, 0.34f), ColRed, TextAnchor.MiddleCenter);
                var clearLayout = clearButton.gameObject.AddComponent<LayoutElement>();
                clearLayout.preferredWidth = 400f;
                clearLayout.minWidth = 400f;
                clearLayout.preferredHeight = 44f;
                clearLayout.minHeight = 44f;
                clearButton.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 44f);
                clearButton.onClick.AddListener(() =>
                {
                    draftEmployeeSelections.Remove(slotId);
                    expandedEmployeeSlotId = null;
                    RefreshDraftPage();
                });
            }
        }

        private List<EmployeeRuntimeData> GetSelectableEmployees(CompanySimulator.Features.Employees.Runtime.Definitions.EmployeeRoleDefinition role, string currentSlotId)
        {
            var result = new List<EmployeeRuntimeData>(8);
            if (employeeManager == null || role == null)
            {
                return result;
            }

            if (draftEmployeeSelections.TryGetValue(currentSlotId, out var currentSelection) && currentSelection != null)
            {
                result.Add(currentSelection);
            }

            if (selectedActiveProject != null)
            {
                var assignedEmployees = selectedActiveProject.AssignedEmployees;
                for (var i = 0; i < assignedEmployees.Count; i++)
                {
                    var employee = assignedEmployees[i];
                    if (employee != null && employee.Role == role && !result.Contains(employee) && !IsSelectedInAnotherSlot(employee, currentSlotId))
                    {
                        result.Add(employee);
                    }
                }
            }

            var idleEmployees = employeeManager.GetIdleEmployeesByRole(role);
            for (var i = 0; i < idleEmployees.Count; i++)
            {
                var employee = idleEmployees[i];
                if (employee == null || result.Contains(employee) || IsSelectedInAnotherSlot(employee, currentSlotId))
                {
                    continue;
                }

                result.Add(employee);
            }

            return result;
        }

        private bool IsSelectedInAnotherSlot(EmployeeRuntimeData employee, string currentSlotId)
        {
            foreach (var pair in draftEmployeeSelections)
            {
                if (pair.Key == currentSlotId)
                {
                    continue;
                }

                if (pair.Value == employee)
                {
                    return true;
                }
            }

            return false;
        }

        private void CreateEmployeeCandidateButton(string slotId, EmployeeRuntimeData employee, Transform parent)
        {
            var accent = GetEmployeeAccent(employee);
            var card = CreateSurface(parent, $"Candidate_{slotId}_{employee.Id}", 128f, ColPanel);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(400f, 128f);
            var cardLayout = card.GetComponent<LayoutElement>();
            cardLayout.preferredWidth = 400f;
            cardLayout.minWidth = 400f;
            AddHoverEffect(card, ColPanel, Blend(ColPanel, accent, 0.18f));
            CreateAccentBar(card.transform, accent);

            var button = card.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.colors = CreateButtonColors(ColPanel, Blend(ColPanel, accent, 0.18f), Darken(ColPanel, 0.12f));

            var content = CreateStretchContainer(card.transform, "Content", 12f, 12f, 12f, 12f);
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 0, 0, 0);
            contentLayout.spacing = 6f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var topRow = CreateUiObject("TopRow", content.transform);
            topRow.AddComponent<LayoutElement>().preferredHeight = 24f;
            var topRowLayout = topRow.AddComponent<HorizontalLayoutGroup>();
            topRowLayout.spacing = 8f;
            topRowLayout.childControlWidth = true;
            topRowLayout.childControlHeight = true;
            topRowLayout.childForceExpandWidth = true;
            topRowLayout.childForceExpandHeight = false;

            var name = CreateText(topRow.transform, employee.DisplayName, 18, TextAnchor.MiddleLeft);
            name.color = ColText;
            name.fontStyle = FontStyle.Bold;

            CreateTag(topRow.transform, employee.QualityTier.ToString(), new Color(accent.r, accent.g, accent.b, 0.18f), accent, 13);

            var role = CreateText(content.transform, employee.Role != null ? employee.Role.DisplayName : "Rol Yok", 14, TextAnchor.MiddleLeft);
            role.color = ColMuted;
            role.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

            var statsRow = CreateUiObject("StatsRow", content.transform);
            statsRow.AddComponent<LayoutElement>().preferredHeight = 42f;
            var statsGrid = statsRow.AddComponent<GridLayoutGroup>();
            statsGrid.cellSize = new Vector2(174f, 42f);
            statsGrid.spacing = new Vector2(8f, 0f);
            statsGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            statsGrid.constraintCount = 2;

            CreateMiniStat(statsRow.transform, employee.ExpectedDailySalary.Amount.ToString("N0"), "Maaþ");
            CreateMiniStat(statsRow.transform, "x" + employee.IncomeMultiplier.ToString("0.0"), "Çarpan");
            button.onClick.AddListener(() =>
            {
                draftEmployeeSelections[slotId] = employee;
                expandedEmployeeSlotId = null;
                RefreshDraftPage();
            });
        }

        private void CreateInvestmentEditors(ProjectExecutionDefinition project)
        {
            var investments = GetEditableInvestments(project);
            var rentInvestment = FindInvestmentById(investments, "kira");
            var purchaseInvestment = FindInvestmentById(investments, "satinalma");

            if (rentInvestment != null && purchaseInvestment != null)
            {
                if (selectedActiveProject != null)
                {
                    CreateInfoCard("Aktif iþte kira / satýn al tercihi deðiþtirilemez. Yalnýzca seçili yatýrýmýn bütçesini artýrabilirsin.", 76f);
                    if (selectedPropertyInvestment == null)
                    {
                        selectedPropertyInvestment = selectedActiveProject.GetCurrentBudgetFor(rentInvestment) > 0 ? rentInvestment : purchaseInvestment;
                    }
                }
                else
                {
                    CreatePropertyChoiceEditor(rentInvestment, purchaseInvestment);
                }
            }

            const float investmentCardWidth = 600f;
            const float investmentCardHeight = 132f;
            const float investmentGridSpacing = 12f;

            var investmentGridHost = CreateUiObject("InvestmentGrid", contentRoot);
            var investmentGrid = investmentGridHost.AddComponent<GridLayoutGroup>();
            investmentGrid.cellSize = new Vector2(investmentCardWidth, investmentCardHeight);
            investmentGrid.spacing = new Vector2(investmentGridSpacing, investmentGridSpacing);
            investmentGrid.padding = new RectOffset(0, 0, 0, 0);
            investmentGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            investmentGrid.constraintCount = CalculateGridColumnCount(investmentCardWidth, investmentGridSpacing);
            investmentGrid.childAlignment = TextAnchor.UpperLeft;

            var investmentGridFitter = investmentGridHost.AddComponent<ContentSizeFitter>();
            investmentGridFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            investmentGridFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var hasVisibleInvestment = false;
            for (var i = 0; i < investments.Count; i++)
            {
                var investment = investments[i];
                if (investment == null)
                {
                    continue;
                }

                if ((investment == rentInvestment || investment == purchaseInvestment) && investment != selectedPropertyInvestment)
                {
                    continue;
                }

                CreateInvestmentEditor(project, investment, investmentGridHost.transform);
                hasVisibleInvestment = true;
            }

            if (!hasVisibleInvestment && rentInvestment == null && purchaseInvestment == null)
            {
                UnityEngine.Object.Destroy(investmentGridHost);
                CreateInfoCard("Bu iþ için yatýrým tanýmý bulunmuyor.");
            }
            else if (!hasVisibleInvestment)
            {
                UnityEngine.Object.Destroy(investmentGridHost);
            }
        }

        private List<InvestmentTypeDefinition> GetEditableInvestments(ProjectExecutionDefinition project)
        {
            var result = new List<InvestmentTypeDefinition>(8);
            if (project == null)
            {
                return result;
            }

            var investments = project.ProjectType != null && project.ProjectType.RecommendedInvestments.Count > 0
                ? project.ProjectType.RecommendedInvestments
                : selectedSector != null ? selectedSector.Sector.AvailableInvestments : Array.Empty<InvestmentTypeDefinition>();

            for (var i = 0; i < investments.Count; i++)
            {
                if (investments[i] != null && !result.Contains(investments[i]))
                {
                    result.Add(investments[i]);
                }
            }

            return result;
        }

        private InvestmentTypeDefinition FindInvestmentById(IReadOnlyList<InvestmentTypeDefinition> investments, string investmentId)
        {
            for (var i = 0; i < investments.Count; i++)
            {
                var investment = investments[i];
                if (investment != null && string.Equals(investment.Id, investmentId, StringComparison.OrdinalIgnoreCase))
                {
                    return investment;
                }
            }

            return null;
        }

        private void CreatePropertyChoiceEditor(InvestmentTypeDefinition rentInvestment, InvestmentTypeDefinition purchaseInvestment)
        {
            CreateInfoCard("Mülk yatýrýmý için kirala ya da satýn al seçeneklerinden birini seçmelisin.", 76f);

            var row = CreateUiObject("PropertyChoiceRow", contentRoot);
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 10f;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 56f);
            var rowLayoutElement = row.AddComponent<LayoutElement>();
            rowLayoutElement.preferredHeight = 56f;

            CreatePropertyChoiceButton(row.transform, rentInvestment, "Kirala");
            CreatePropertyChoiceButton(row.transform, purchaseInvestment, "Satýn Al");
        }

        private void CreatePropertyChoiceButton(Transform parent, InvestmentTypeDefinition investment, string label)
        {
            var isSelected = selectedPropertyInvestment == investment;
            var accent = isSelected ? ColCyan : ColSurfaceAlt;
            var button = CreateStyledButton(parent, "PropertyChoice_" + investment.Id, isSelected ? label + " (Seçili)" : label, accent, Blend(accent, ColBlue, 0.16f), Darken(accent, 0.12f), isSelected ? ColText : ColMuted, TextAnchor.MiddleCenter);
            var layoutElement = button.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 180f;
            layoutElement.minWidth = 180f;
            layoutElement.preferredHeight = 56f;
            layoutElement.minHeight = 56f;
            button.onClick.AddListener(() =>
            {
                selectedPropertyInvestment = investment;
                RefreshDraftPage();
            });
        }

        private void CreateInvestmentEditor(ProjectExecutionDefinition project, InvestmentTypeDefinition investment, Transform parent)
        {
            if (!draftBudgetCache.ContainsKey(investment))
            {
                draftBudgetCache[investment] = GetDefaultBudget(project, investment);
            }

            var accent = GetInvestmentAccent(investment);
            var card = CreateSurface(parent, $"Investment_{investment.Id}", 132f, ColPanel);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(600f, 132f);
            var cardLayout = card.GetComponent<LayoutElement>();
            cardLayout.preferredWidth = 600f;
            cardLayout.minWidth = 600f;
            AddHoverEffect(card, ColPanel, Blend(ColPanel, accent, 0.16f));
            CreateAccentBar(card.transform, accent);

            var content = CreateStretchContainer(card.transform, "Content", 14f, 12f, 14f, 12f);
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 0, 0, 0);
            contentLayout.spacing = 6f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var topRow = CreateUiObject("TopRow", content.transform);
            topRow.AddComponent<LayoutElement>().preferredHeight = 26f;
            var topLayout = topRow.AddComponent<HorizontalLayoutGroup>();
            topLayout.spacing = 8f;
            topLayout.childControlWidth = true;
            topLayout.childControlHeight = true;
            topLayout.childForceExpandWidth = true;
            topLayout.childForceExpandHeight = false;

            var nameText = CreateText(topRow.transform, investment.DisplayName, 20, TextAnchor.MiddleLeft);
            nameText.color = ColText;
            nameText.fontStyle = FontStyle.Bold;

            CreateTag(topRow.transform, investment.IsRecurringExpense ? "Gelirden Düþer" : "Peþin", new Color(accent.r, accent.g, accent.b, 0.18f), accent, 13);

            var minimumBudget = GetMinimumAllowedBudget(project, investment);
            var maximumBudget = GetMaximumAllowedBudget(investment);
            var detailText = CreateText(
                content.transform,
                selectedActiveProject == null
                    ? $"Minimum: {investment.MinimumBudget:N0} | Önerilen: {investment.RecommendedBudget:N0} | Maksimum: {maximumBudget:N0}"
                    : $"Mevcut: {minimumBudget:N0} | Maksimum: {maximumBudget:N0} | Sadece artýþ yapýlabilir",
                14,
                TextAnchor.MiddleLeft);
            detailText.color = ColMuted;
            detailText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

            var bottomRow = CreateUiObject("BottomRow", content.transform);
            bottomRow.AddComponent<LayoutElement>().preferredHeight = 50f;
            var bottomLayout = bottomRow.AddComponent<HorizontalLayoutGroup>();
            bottomLayout.spacing = 10f;
            bottomLayout.childControlWidth = true;
            bottomLayout.childControlHeight = true;
            bottomLayout.childForceExpandWidth = true;
            bottomLayout.childForceExpandHeight = false;
            bottomLayout.childAlignment = TextAnchor.MiddleLeft;

            var inputField = CreateInputField(bottomRow.transform, draftBudgetCache[investment].ToString());
            var inputRect = inputField.GetComponent<RectTransform>();
            inputRect.sizeDelta = new Vector2(180f, 40f);
            var inputLayout = inputField.gameObject.AddComponent<LayoutElement>();
            inputLayout.preferredWidth = 180f;
            inputLayout.minWidth = 180f;
            inputLayout.preferredHeight = 40f;
            inputLayout.minHeight = 40f;

            var evaluationHost = CreateUiObject("EvaluationHost", bottomRow.transform);
            var evaluationHostLayout = evaluationHost.AddComponent<LayoutElement>();
            evaluationHostLayout.preferredHeight = 30f;
            evaluationHostLayout.flexibleWidth = 1f;

            var evaluationLayout = evaluationHost.AddComponent<HorizontalLayoutGroup>();
            evaluationLayout.childAlignment = TextAnchor.MiddleRight;
            evaluationLayout.childControlWidth = false;
            evaluationLayout.childControlHeight = true;
            evaluationLayout.childForceExpandWidth = false;
            evaluationLayout.childForceExpandHeight = false;

            var evaluationTag = CreateTag(evaluationHost.transform, string.Empty, new Color(accent.r, accent.g, accent.b, 0.16f), accent, 13);
            var evaluationText = evaluationTag.GetComponentInChildren<Text>();

            void RefreshEvaluation(string inputValue)
            {
                var parsedBudget = ParseBudget(inputValue, minimumBudget, maximumBudget);
                draftBudgetCache[investment] = parsedBudget;
                inputField.text = parsedBudget.ToString();
                evaluationText.text = investment.GetBudgetEvaluationLabel(parsedBudget);
            }

            RefreshEvaluation(inputField.text);
            inputField.onEndEdit.AddListener(RefreshEvaluation);
        }

        private void PreviewDraft()
        {
            if (draftResultText == null || economyManager == null)
            {
                return;
            }

            if (!TryBuildDraftRequest(out var request, out var validationMessage))
            {
                draftResultText.text = validationMessage;
                return;
            }

            var result = economyManager.PreviewProject(request);
            var isNewProject = selectedActiveProject == null;
            draftResultText.text = BuildResultSummary(result, simulateExtraProject: isNewProject);
        }

        private void StartDraft()
        {
            if (draftResultText == null || economyManager == null)
            {
                return;
            }

            if (!TryBuildDraftRequest(out var request, out var validationMessage))
            {
                draftResultText.text = validationMessage;
                return;
            }

            if (companyAccountingManager != null && !companyAccountingManager.CanCreateAdditionalProject(out var accountingValidationMessage))
            {
                draftResultText.text = accountingValidationMessage;
                return;
            }

            if (selectedActiveProject != null)
            {
                ApplyActiveProjectChanges(request);
                return;
            }

            var selectedEmployees = GetSelectedEmployees(selectedProjectTemplate);
            var assignedEmployeeSlotIds = BuildAssignedEmployeeSlotIds(selectedProjectTemplate);
            if (employeeManager != null && !employeeManager.CanAssignEmployees(selectedEmployees))
            {
                draftResultText.text = "Seçtiðin çalýþanlardan en az biri artýk boþta deðil. Lütfen çalýþan seçimini yenile.";
                ShowNewJobPage(selectedSector);
                return;
            }

            var displayName = selectedProjectTemplate != null ? selectedProjectTemplate.DisplayName : request.ProjectType.DisplayName;
            var assignedEmployeeNames = BuildAssignedEmployeeNames(selectedEmployees);
            if (economyManager.TryExecuteProject(selectedProjectTemplate, request, displayName, selectedEmployees, assignedEmployeeSlotIds, assignedEmployeeNames, out var result))
            {
                draftEmployeeSelections.Clear();
                expandedEmployeeSlotId = null;

                if (employeeManager != null && !employeeManager.TryAssignEmployees(selectedEmployees, displayName))
                {
                    draftResultText.text = "Ýþ baþlatýldý fakat çalýþan kilidi uygulanamadý. Lütfen çalýþan durumlarýný kontrol et.";
                    return;
                }

                ShowNewJobPage(selectedSector);
                if (draftResultText != null)
                {
                    draftResultText.text = "Ýþ baþlatýldý.\n\n" + BuildResultSummary(result);
                }
            }
            else
            {
                draftResultText.text = "Ýþ baþlatýlamadý. Muhtemelen bakiye yetersiz.\n\n" + BuildResultSummary(economyManager.PreviewProject(request), simulateExtraProject: true);
            }

        }

        private void ApplyActiveProjectChanges(ProjectEconomyRequest request)
        {
            if (selectedActiveProject == null || draftResultText == null || economyManager == null)
            {
                return;
            }

            var selectedEmployees = GetSelectedEmployees(selectedProjectTemplate);
            var assignedEmployeeSlotIds = BuildAssignedEmployeeSlotIds(selectedProjectTemplate);
            var currentAssignedEmployees = selectedActiveProject.AssignedEmployees;
            if (employeeManager != null && !employeeManager.CanReassignEmployees(currentAssignedEmployees, selectedEmployees))
            {
                draftResultText.text = "Seçtiðin çalýþanlardan en az biri baþka bir iþte çalýþýyor. Lütfen çalýþan seçimini yenile.";
                RefreshAll();
                return;
            }

            var assignedEmployeeNames = BuildAssignedEmployeeNames(selectedEmployees);
            if (!economyManager.TryUpdateActiveProject(selectedActiveProject, request, selectedEmployees, assignedEmployeeSlotIds, assignedEmployeeNames, out var result, out var validationMessage))
            {
                draftResultText.text = validationMessage;
                return;
            }

            if (employeeManager != null && !employeeManager.TryReassignEmployees(currentAssignedEmployees, selectedEmployees, selectedActiveProject.DisplayName))
            {
                draftResultText.text = "Aktif iþ güncellendi fakat çalýþan atamalarý yenilenemedi. Lütfen çalýþan durumlarýný kontrol et.";
                return;
            }

            LoadDraftFromActiveProject(selectedActiveProject);
            RefreshAll();
            if (draftResultText != null)
            {
                draftResultText.text = "Aktif iþ güncellendi. Gelir döngüsü sýfýrlandý.\n\n" + BuildResultSummary(result);
            }
        }

        private bool TryBuildDraftRequest(out ProjectEconomyRequest request, out string validationMessage)
        {
            request = null;
            validationMessage = string.Empty;

            if (selectedProjectTemplate == null)
            {
                validationMessage = "Ýþ baþlatmak için geçerli bir iþ þablonu seçilmedi.";
                return false;
            }

            var projectType = selectedProjectTemplate.ProjectType;
            if (projectType == null)
            {
                validationMessage = "Seçilen iþ þablonunun proje tipi eksik.";
                return false;
            }

            if (!TryBuildEmployeeAssignments(selectedProjectTemplate, out var employeeAssignments, out validationMessage))
            {
                return false;
            }

            if (!TryBuildInvestmentAllocations(selectedProjectTemplate, out var investmentAllocations, out validationMessage))
            {
                return false;
            }

            request = new ProjectEconomyRequest(
                projectType,
                employeeAssignments,
                investmentAllocations,
                selectedProjectTemplate.MarketDemandMultiplier,
                selectedProjectTemplate.CompetitorPressure);
            return true;
        }

        private bool TryBuildEmployeeAssignments(ProjectExecutionDefinition project, out List<EmployeeAssignmentInput> assignments, out string validationMessage)
        {
            assignments = new List<EmployeeAssignmentInput>(8);
            validationMessage = string.Empty;

            var templateAssignments = project.EmployeeAssignments;
            for (var assignmentIndex = 0; assignmentIndex < templateAssignments.Count; assignmentIndex++)
            {
                var templateAssignment = templateAssignments[assignmentIndex];
                var role = templateAssignment.Role;
                if (role == null)
                {
                    continue;
                }

                var totalQuality = 0f;
                var totalContributionMultiplier = 0f;
                var requiredCount = Mathf.Max(0, templateAssignment.Count);
                if (requiredCount <= 0)
                {
                    continue;
                }

                for (var slotIndex = 0; slotIndex < requiredCount; slotIndex++)
                {
                    var slotId = BuildEmployeeSlotId(role, assignmentIndex, slotIndex);
                    if (!draftEmployeeSelections.TryGetValue(slotId, out var selectedEmployee) || selectedEmployee == null)
                    {
                        validationMessage = $"{role.DisplayName} için tüm çalýþan kutularýný doldurmalýsýn.";
                        return false;
                    }

                    totalQuality += selectedEmployee.Quality;
                    totalContributionMultiplier += selectedEmployee.IncomeMultiplier;
                }

                assignments.Add(new EmployeeAssignmentInput(
                    role,
                    requiredCount,
                    totalQuality / requiredCount,
                    totalContributionMultiplier / requiredCount));
            }

            return true;
        }

        private bool TryBuildInvestmentAllocations(ProjectExecutionDefinition project, out List<InvestmentAllocationInput> allocations, out string validationMessage)
        {
            allocations = new List<InvestmentAllocationInput>(8);
            validationMessage = string.Empty;

            var investments = GetEditableInvestments(project);
            var rentInvestment = FindInvestmentById(investments, "kira");
            var purchaseInvestment = FindInvestmentById(investments, "satinalma");
            if (rentInvestment != null && purchaseInvestment != null && selectedPropertyInvestment == null)
            {
                validationMessage = "Kira ya da satýn al seçeneklerinden birini seçmelisin.";
                return false;
            }

            for (var i = 0; i < investments.Count; i++)
            {
                var investment = investments[i];
                if (investment == null)
                {
                    continue;
                }

                if ((investment == rentInvestment || investment == purchaseInvestment) && investment != selectedPropertyInvestment)
                {
                    continue;
                }

                var budget = draftBudgetCache.TryGetValue(investment, out var cachedBudget)
                    ? cachedBudget
                    : GetDefaultBudget(project, investment);
                var minimumAllowedBudget = GetMinimumAllowedBudget(project, investment);
                var maximumAllowedBudget = GetMaximumAllowedBudget(investment);
                budget = Mathf.Clamp(budget, minimumAllowedBudget, maximumAllowedBudget);
                draftBudgetCache[investment] = budget;

                allocations.Add(new InvestmentAllocationInput(investment, budget));
            }

            return true;
        }

        private string BuildEmployeeSlotId(CompanySimulator.Features.Employees.Runtime.Definitions.EmployeeRoleDefinition role, int assignmentIndex, int slotIndex)
        {
            return $"{role?.Id ?? "role"}_{assignmentIndex}_{slotIndex}";
        }

        private List<EmployeeRuntimeData> GetSelectedEmployees(ProjectExecutionDefinition project)
        {
            var result = new List<EmployeeRuntimeData>(draftEmployeeSelections.Count);
            if (project == null)
            {
                return result;
            }

            var assignments = project.EmployeeAssignments;
            for (var assignmentIndex = 0; assignmentIndex < assignments.Count; assignmentIndex++)
            {
                var assignment = assignments[assignmentIndex];
                var requiredCount = Mathf.Max(0, assignment.Count);
                for (var slotIndex = 0; slotIndex < requiredCount; slotIndex++)
                {
                    var slotId = BuildEmployeeSlotId(assignment.Role, assignmentIndex, slotIndex);
                    if (draftEmployeeSelections.TryGetValue(slotId, out var selectedEmployee) && selectedEmployee != null)
                    {
                        result.Add(selectedEmployee);
                    }
                }
            }

            return result;
        }

        private List<string> BuildAssignedEmployeeSlotIds(ProjectExecutionDefinition project)
        {
            var result = new List<string>(draftEmployeeSelections.Count);
            if (project == null)
            {
                return result;
            }

            var assignments = project.EmployeeAssignments;
            for (var assignmentIndex = 0; assignmentIndex < assignments.Count; assignmentIndex++)
            {
                var assignment = assignments[assignmentIndex];
                var requiredCount = Mathf.Max(0, assignment.Count);
                for (var slotIndex = 0; slotIndex < requiredCount; slotIndex++)
                {
                    var slotId = BuildEmployeeSlotId(assignment.Role, assignmentIndex, slotIndex);
                    if (draftEmployeeSelections.TryGetValue(slotId, out var selectedEmployee) && selectedEmployee != null)
                    {
                        result.Add(slotId);
                    }
                }
            }

            return result;
        }

        private List<string> BuildAssignedEmployeeNames(IReadOnlyList<EmployeeRuntimeData> employees)
        {
            var result = new List<string>(employees.Count);
            for (var i = 0; i < employees.Count; i++)
            {
                if (employees[i] != null)
                {
                    result.Add(employees[i].DisplayName);
                }
            }

            return result;
        }

        private string BuildResultSummary(ProjectEconomyResult result, SectorDefinition sector = null, bool simulateExtraProject = false)
        {
            var baseRevenue = result.Revenue;
            if (sector == null && selectedSector != null)
            {
                sector = selectedSector.Sector;
            }

            var multiplier = simulateExtraProject
                ? SectorCompetitionService.GetCachedRevenueMultiplierWithExtra(sector, 1)
                : SectorCompetitionService.GetCachedRevenueMultiplier(sector);
            var adjustedRevenue = Money.From(baseRevenue.Amount * multiplier);
            var adjustedProfit = adjustedRevenue - result.PayrollCost - result.RecurringInvestmentCost;
            return $"Tahmini Gelir: {adjustedRevenue.Amount:N0} | Tahmini Kâr: {adjustedProfit.Amount:N0}\n(Rekabet Çarpaný: {multiplier:P0})";
        }

        private Money GetCycleProfit(ProjectEconomyResult result)
        {
            return result.Revenue - result.PayrollCost - result.RecurringInvestmentCost;
        }

        private int GetMinimumAllowedBudget(ProjectExecutionDefinition project, InvestmentTypeDefinition investment)
        {
            var activeBudget = selectedActiveProject != null ? selectedActiveProject.GetCurrentBudgetFor(investment) : 0;
            return Mathf.Max(investment.MinimumBudget, activeBudget);
        }

        private int GetDefaultBudget(ProjectExecutionDefinition project, InvestmentTypeDefinition investment)
        {
            var templateBudget = project.GetAllocatedBudgetFor(investment);
            if (templateBudget > 0)
            {
                return Mathf.Min(templateBudget, GetMaximumAllowedBudget(investment));
            }

            return Mathf.Clamp(Mathf.Max(investment.MinimumBudget, investment.RecommendedBudget), investment.MinimumBudget, GetMaximumAllowedBudget(investment));
        }

        private int GetMaximumAllowedBudget(InvestmentTypeDefinition investment)
        {
            return investment != null ? investment.MaximumBudget : 0;
        }

        private int ParseBudget(string inputValue, int minimumValue, int maximumValue)
        {
            var fallbackValue = Mathf.Clamp(minimumValue, minimumValue, maximumValue);
            var parsedValue = int.TryParse(inputValue, out var parsedBudget) && parsedBudget >= 0 ? parsedBudget : fallbackValue;
            return Mathf.Clamp(parsedValue, minimumValue, maximumValue);
        }

        private void CreateSectionTitle(string title)
        {
            var titleText = CreateText(contentRoot, title, 20, TextAnchor.MiddleLeft);
            titleText.rectTransform.sizeDelta = new Vector2(0f, 34f);
            titleText.color = ColText;
            titleText.fontStyle = FontStyle.Bold;
        }

        private Text CreateInfoCard(string message, float height = 58f)
        {
            return CreateInfoCard(contentRoot, message, height);
        }

        private Text CreateInfoCard(Transform parent, string message, float height = 58f)
        {
            var card = CreateSurface(parent, "InfoCard", height, ColSurface);
            var text = CreateText(card.transform, message, 18, TextAnchor.MiddleLeft);
            text.color = ColMuted;
            StretchToParent(text.rectTransform, 14f, 8f, 14f, 8f);
            return text;
        }

        private GameObject CreateUiObject(string objectName, Transform parent)
        {
            return RuntimePanelUiUtility.CreateUiObject(objectName, parent);
        }

        private Button CreateButton(Transform parent, string objectName, string label)
        {
            return CreateStyledButton(parent, objectName, label, ColSurface, Blend(ColSurface, ColBlue, 0.16f), Darken(ColSurface, 0.12f), ColText, TextAnchor.MiddleLeft);
        }

        private InputField CreateInputField(Transform parent, string initialValue)
        {
            var inputField = RuntimePanelUiUtility.CreateInputField(parent, defaultFont, initialValue);
            ApplyRoundedImage(inputField.gameObject, ColSurfaceAlt);
            var placeholder = inputField.placeholder as Text;
            if (placeholder != null)
            {
                placeholder.color = new Color(ColMuted.r, ColMuted.g, ColMuted.b, 0.45f);
            }

            if (inputField.textComponent != null)
            {
                inputField.textComponent.color = ColText;
            }

            return inputField;
        }

        private GameObject CreateTag(Transform parent, string value, Color bgColor, Color textColor, int fontSize = 12)
        {
            var tag = CreateUiObject("Tag", parent);
            ApplyRoundedImage(tag, bgColor);

            var tagLayout = tag.AddComponent<LayoutElement>();
            tagLayout.preferredHeight = fontSize >= 14 ? 30f : 26f;

            var layout = tag.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 4, 4);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = tag.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var text = CreateText(tag.transform, value, fontSize, TextAnchor.MiddleCenter);
            text.color = textColor;
            text.fontStyle = FontStyle.Bold;
            var textLayout = text.gameObject.AddComponent<LayoutElement>();
            textLayout.preferredHeight = fontSize >= 14 ? 18f : 16f;
            return tag;
        }

        private GameObject CreateMiniStat(Transform parent, string value, string label)
        {
            var tile = CreateSurface(parent, "MiniStat", 46f, new Color(ColSurfaceAlt.r, ColSurfaceAlt.g, ColSurfaceAlt.b, 0.95f));
            var valueText = CreateText(tile.transform, value, 17, TextAnchor.UpperCenter);
            valueText.color = ColText;
            valueText.fontStyle = FontStyle.Bold;
            StretchToParent(valueText.rectTransform, 6f, 18f, 6f, 3f);

            var labelText = CreateText(tile.transform, label, 11, TextAnchor.LowerCenter);
            labelText.color = ColMuted;
            StretchToParent(labelText.rectTransform, 6f, 3f, 6f, 21f);
            return tile;
        }

        private Transform EnsureActiveProjectGridHost()
        {
            if (activeProjectGridParent != null)
            {
                return activeProjectGridParent;
            }

            const float projectCardWidth = 400f;
            const float projectCardHeight = 228f;
            const float projectGridSpacing = 12f;

            var host = CreateUiObject("ActiveProjectGrid", contentRoot);
            var grid = host.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(projectCardWidth, projectCardHeight);
            grid.spacing = new Vector2(projectGridSpacing, projectGridSpacing);
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = CalculateGridColumnCount(projectCardWidth, projectGridSpacing);
            grid.childAlignment = TextAnchor.UpperLeft;

            var fitter = host.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            activeProjectGridParent = host.transform;
            return activeProjectGridParent;
        }

        private GameObject CreateCenteredCardRow(Transform parent, float height)
        {
            var row = CreateUiObject("CenteredCardRow", parent);
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childAlignment = TextAnchor.UpperCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            var layout = row.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.minHeight = height;
            return row;
        }

        private GameObject CreateLeftAlignedCardRow(Transform parent, float height)
        {
            var row = CreateUiObject("LeftAlignedCardRow", parent);
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childAlignment = TextAnchor.UpperLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            var layout = row.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.minHeight = height;
            return row;
        }

        private Transform EnsureSectorListGridHost()
        {
            if (sectorListGridParent != null)
            {
                return sectorListGridParent;
            }

            const float sectorCardWidth = 400f;
            const float sectorCardHeight = 186f;
            const float sectorGridSpacing = 12f;

            var gridHost = CreateUiObject("SectorGrid", contentRoot);
            var grid = gridHost.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(sectorCardWidth, sectorCardHeight);
            grid.spacing = new Vector2(sectorGridSpacing, sectorGridSpacing);
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = CalculateGridColumnCount(sectorCardWidth, sectorGridSpacing);
            grid.childAlignment = TextAnchor.UpperCenter;

            sectorListGridParent = gridHost.transform;
            return sectorListGridParent;
        }

        private Color GetEmployeeAccent(EmployeeRuntimeData employee)
        {
            if (employee == null)
            {
                return ColBlue;
            }

            var qualityKey = employee.QualityTier.ToString();
            switch (qualityKey)
            {
                case "Low":
                    return ColGreen;
                case "Mid":
                    return ColGold;
                case "High":
                    return ColPurple;
                default:
                    return ColBlue;
            }
        }

        private int CalculateGridColumnCount(float cardWidth, float spacing)
        {
            const float horizontalPadding = 80f;
            var availableWidth = Mathf.Max(cardWidth, panelSize.x - horizontalPadding);
            return Mathf.Max(1, Mathf.FloorToInt((availableWidth + spacing) / (cardWidth + spacing)));
        }

        private static float CalculateGridHeight(int itemCount, int columnCount, float cardHeight, float spacing)
        {
            var safeColumnCount = Mathf.Max(1, columnCount);
            var rowCount = Mathf.CeilToInt(itemCount / (float)safeColumnCount);
            return rowCount * cardHeight + Mathf.Max(0, rowCount - 1) * spacing;
        }

        private Color GetInvestmentAccent(InvestmentTypeDefinition investment)
        {
            if (investment == null)
            {
                return ColBlue;
            }

            if (investment.IsRecurringExpense)
            {
                return ColGold;
            }

            var key = investment.Id ?? investment.DisplayName ?? string.Empty;
            return Mathf.Abs(key.GetHashCode()) % 2 == 0 ? ColCyan : ColBlue;
        }

        private Color GetSectorAccent(SectorDefinition sector)
        {
            var key = sector != null ? sector.Id ?? sector.DisplayName ?? string.Empty : string.Empty;
            switch (Mathf.Abs(key.GetHashCode()) % 6)
            {
                case 0:
                    return ColGreen;
                case 1:
                    return ColGold;
                case 2:
                    return ColRed;
                case 3:
                    return ColPurple;
                case 4:
                    return ColCyan;
                default:
                    return ColBlue;
            }
        }

        private Text CreateText(Transform parent, string value, int fontSize, TextAnchor anchor)
        {
            return RuntimePanelUiUtility.CreateText(parent, defaultFont, value, fontSize, anchor);
        }

        private void RefreshAgentButtons()
        {
            if (agentDismissButton != null && agentManager != null)
            {
                var detectedCount = agentManager.GetDetectedAgentCount();
                agentDismissButton.gameObject.SetActive(detectedCount > 0);
            }
            else if (agentDismissButton != null)
            {
                agentDismissButton.gameObject.SetActive(false);
            }
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

        private void RefreshDayText()
        {
            if (dayText == null)
            {
                return;
            }

            var day = economyManager != null ? economyManager.CurrentDay : 1;
            dayText.text = $"Gün: {day}";
        }

        private Font LoadDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private GameObject CreateSurface(Transform parent, string name, float height, Color color)
        {
            var surface = CreateUiObject(name, parent);
            var rect = surface.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, height);
            ApplyRoundedImage(surface, color);

            var layout = surface.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = surface.AddComponent<LayoutElement>();
            }

            layout.preferredHeight = height;
            layout.minHeight = height;
            return surface;
        }

        private GameObject CreateRoundedBlock(Transform parent, string name, Vector2 size, Color color)
        {
            var block = CreateUiObject(name, parent);
            var rect = block.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            ApplyRoundedImage(block, color);
            return block;
        }

        private GameObject CreateStretchContainer(Transform parent, string name, float left, float bottom, float right, float top)
        {
            var container = CreateUiObject(name, parent);
            StretchToParent(container.GetComponent<RectTransform>(), left, bottom, right, top);
            IgnoreLayout(container);
            return container;
        }

        private GameObject CreateFlexibleSpacer(Transform parent)
        {
            var spacer = CreateUiObject("Spacer", parent);
            var layout = spacer.AddComponent<LayoutElement>();
            layout.flexibleWidth = 1f;
            layout.flexibleHeight = 1f;
            return spacer;
        }

        private Button CreateStyledButton(Transform parent, string objectName, string label, Color normal, Color hover, Color pressed, Color textColor, TextAnchor anchor)
        {
            var buttonObject = CreateUiObject(objectName, parent);
            ApplyRoundedImage(buttonObject, normal);
            AddHoverEffect(buttonObject, normal, hover);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            button.colors = CreateButtonColors(normal, hover, pressed);

            var text = CreateText(buttonObject.transform, label, 18, anchor);
            text.color = textColor;
            text.fontStyle = FontStyle.Bold;
            StretchToParent(text.rectTransform, 16f, 8f, 16f, 8f);
            return button;
        }

        private void ApplyRoundedImage(GameObject target, Color color)
        {
            var image = target.GetComponent<Image>();
            if (image == null)
            {
                image = target.AddComponent<Image>();
            }

            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = color;
        }

        private static void EnsureRoundedMask(GameObject target)
        {
            var mask = target.GetComponent<Mask>();
            if (mask == null)
            {
                mask = target.AddComponent<Mask>();
            }

            mask.showMaskGraphic = true;
        }

        private void CreateAccentBar(Transform parent, Color color)
        {
            var bar = CreateUiObject("AccentBar", parent);
            IgnoreLayout(bar);
            var rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = new Vector2(10f, -3f);
            rect.offsetMax = new Vector2(-10f, 0f);
            bar.AddComponent<Image>().color = color;
        }

        private static void IgnoreLayout(GameObject target)
        {
            var layout = target.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = target.AddComponent<LayoutElement>();
            }

            layout.ignoreLayout = true;
        }

        private void AddHoverEffect(GameObject target, Color normalColor, Color hoverColor)
        {
            var trigger = target.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = target.AddComponent<EventTrigger>();
            }

            var image = target.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ =>
            {
                image.color = hoverColor;
                target.transform.localScale = new Vector3(1.01f, 1.01f, 1f);
            });
            trigger.triggers.Add(enter);

            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ =>
            {
                image.color = normalColor;
                target.transform.localScale = Vector3.one;
            });
            trigger.triggers.Add(exit);
        }

        private static ColorBlock CreateButtonColors(Color normal, Color hover, Color pressed)
        {
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = normal;
            colors.highlightedColor = hover;
            colors.pressedColor = pressed;
            colors.selectedColor = hover;
            colors.disabledColor = new Color(0.15f, 0.15f, 0.2f, 0.6f);
            colors.fadeDuration = 0.1f;
            return colors;
        }

        private static Color Blend(Color a, Color b, float t)
        {
            return Color.Lerp(a, b, Mathf.Clamp01(t));
        }

        private static Color Darken(Color color, float amount)
        {
            return Color.Lerp(color, Color.black, Mathf.Clamp01(amount));
        }

        private static Sprite LoadRoundedSprite()
        {
            const int size = 128;
            const int radius = 24;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "GeneratedRoundedSprite",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var pixels = new Color32[size * size];
            var transparent = new Color32(255, 255, 255, 0);
            var solid = new Color32(255, 255, 255, 255);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    pixels[(y * size) + x] = IsInsideRoundedRect(x, y, size, radius) ? solid : transparent;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
        }

        private static bool IsInsideRoundedRect(int x, int y, int size, int radius)
        {
            var left = x;
            var right = (size - 1) - x;
            var bottom = y;
            var top = (size - 1) - y;

            if ((left >= radius && right >= radius) || (bottom >= radius && top >= radius))
            {
                return true;
            }

            var nearestX = Mathf.Min(left, right);
            var nearestY = Mathf.Min(bottom, top);
            var dx = radius - nearestX - 0.5f;
            var dy = radius - nearestY - 0.5f;
            return (dx * dx) + (dy * dy) <= radius * radius;
        }

        private void UpdateHeaderButtons()
        {
            if (backButton != null)
            {
                backButton.gameObject.SetActive(currentPage != PageState.SectorList);
            }
        }

        private void StretchToParent(RectTransform rectTransform, float left, float bottom, float right, float top)
        {
            RuntimePanelUiUtility.StretchToParent(rectTransform, left, bottom, right, top);
        }

        private void ClearChildren(RectTransform parent)
        {
            RuntimePanelUiUtility.ClearChildren(parent);
        }

        private static T[] ToArray<T>(IReadOnlyList<T> source)
        {
            var result = new T[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                result[i] = source[i];
            }

            return result;
        }

        private void SetDefinitionIdentity(DefinitionBase definition, string id, string displayName)
        {
            SetField(definition, "id", id);
            SetField(definition, "displayName", displayName);
        }

        private void SetField(object target, string fieldName, object value)
        {
            var currentType = target.GetType();
            while (currentType != null)
            {
                var field = currentType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                currentType = currentType.BaseType;
            }

            throw new MissingFieldException(target.GetType().Name, fieldName);
        }

        private enum PageState
        {
            SectorList = 0,
            SectorDetails = 1,
            NewJob = 2,
            ActiveProjectEdit = 3
        }
    }
}
