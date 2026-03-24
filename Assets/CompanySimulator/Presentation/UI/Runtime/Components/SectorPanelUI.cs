using System;
using System.Collections.Generic;
using System.Reflection;
using CompanySimulator.Features.Accounting.Runtime.Components;
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
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private Vector2 panelSize = new Vector2(760f, 720f);

        private readonly Dictionary<InvestmentTypeDefinition, int> draftBudgetCache = new Dictionary<InvestmentTypeDefinition, int>(8);
        private readonly Dictionary<string, EmployeeRuntimeData> draftEmployeeSelections = new Dictionary<string, EmployeeRuntimeData>(16);

        private Font defaultFont;
        private GameObject panelRoot;
        private RectTransform contentRoot;
        private Text balanceText;
        private Text dayText;
        private Text pageTitleText;
        private Text draftResultText;
        private Button backButton;
        private SectorRuntimeData selectedSector;
        private ProjectExecutionDefinition selectedProjectTemplate;
        private ActiveProjectRuntimeEntry selectedActiveProject;
        private InvestmentTypeDefinition selectedPropertyInvestment;
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

            EnsureCanvas();
            EnsureEventSystem();
            defaultFont = LoadDefaultFont();
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
        }

        private void HandleBalanceChanged(Money _)
        {
            RefreshBalanceText();
        }

        private void HandleDayAdvanced(int _)
        {
            RefreshDayText();
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

            var background = balanceRoot.AddComponent<Image>();
            background.color = new Color(0.12f, 0.12f, 0.16f, 0.92f);

            balanceText = CreateText(balanceRoot.transform, "Para: 0", 22, TextAnchor.MiddleLeft);
            StretchToParent(balanceText.rectTransform, 14f, 6f, 14f, 6f);
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

        private void CreateDayWidget()
        {
            dayText = CreateText(rootCanvas.transform, "Gün: 1", 22, TextAnchor.MiddleRight);
            var dayRect = dayText.GetComponent<RectTransform>();
            dayRect.anchorMin = new Vector2(1f, 1f);
            dayRect.anchorMax = new Vector2(1f, 1f);
            dayRect.pivot = new Vector2(1f, 1f);
            dayRect.anchoredPosition = new Vector2(-220f, -20f);
            dayRect.sizeDelta = new Vector2(180f, 48f);

            var nextDayButton = CreateButton(rootCanvas.transform, "NextDayButton", "Sonraki Gün");
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
            StretchToParent(pageTitleText.rectTransform, 18f, 8f, 140f, 8f);

            backButton = CreateButton(headerRoot.transform, "BackButton", "?");
            var backRect = backButton.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(1f, 0.5f);
            backRect.anchorMax = new Vector2(1f, 0.5f);
            backRect.pivot = new Vector2(1f, 0.5f);
            backRect.anchoredPosition = new Vector2(-72f, 0f);
            backRect.sizeDelta = new Vector2(50f, 40f);
            backButton.onClick.AddListener(GoBack);

            var closeButton = CreateButton(headerRoot.transform, "CloseButton", "×");
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

            var scrollImage = scrollRoot.AddComponent<Image>();
            scrollImage.color = new Color(0.13f, 0.15f, 0.19f, 0.92f);

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
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childControlWidth = true;
            layout.childControlHeight = false;
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
            ClearChildren(contentRoot);

            SectorDetailsPage.Render(sectorData, economyManager, message => CreateInfoCard(message), (message, height) => CreateInfoCard(message, height), CreateSectionTitle, CreateActiveProjectCards);

            var newJobButton = CreateButton(contentRoot, "NewJobButton", "+ Yeni Ýþ");
            newJobButton.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 62f);
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
            var previewButton = CreateButton(contentRoot, "PreviewButton", "Önizleme Yap");
            previewButton.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 56f);
            previewButton.onClick.AddListener(PreviewDraft);

            draftResultText = CreateInfoCard("Çalýþan seçip yatýrým tutarlarýný girdikten sonra önizleme yapabilirsin.", 148f);

            var startButton = CreateButton(contentRoot, "StartButton", "Ýþi Baþlat");
            startButton.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 62f);
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
            var previewButton = CreateButton(contentRoot, "PreviewActiveButton", "Deðiþikliði Önizle");
            previewButton.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 56f);
            previewButton.onClick.AddListener(PreviewDraft);

            draftResultText = CreateInfoCard("Çalýþan veya bütçe artýþýný yaptýktan sonra önizleme alabilirsin.", 132f);

            var updateButton = CreateButton(contentRoot, "UpdateActiveJobButton", "Deðiþiklikleri Uygula");
            updateButton.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 62f);
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
            var label = $"{sectorData.Sector.DisplayName}\nAktif Ýþ: {sectorData.ActiveProjectCount}";
            var button = CreateButton(contentRoot, $"Sector_{sectorData.Sector.Id}", label);
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 78f);
            button.onClick.AddListener(() => ShowSectorDetails(sectorData));
        }

        private void CreateActiveProjectCards(SectorRuntimeData sectorData)
        {
            SectorDetailsPage.RenderActiveProjects(sectorData, economyManager, CreateActiveProjectCard, (message, height) => CreateInfoCard(message, height));
        }

        private void CreateActiveProjectCard(ActiveProjectRuntimeEntry activeProject)
        {
            var employeeNames = activeProject.AssignedEmployeeNames.Count > 0
                ? string.Join(", ", activeProject.AssignedEmployeeNames)
                : "Atama bilgisi yok";
            var remainingDays = economyManager != null ? activeProject.DaysUntilNextPayout(economyManager.CurrentDay) : 0;
            var button = CreateButton(
                contentRoot,
                $"ActiveProject_{activeProject.DisplayName}",
                $"{activeProject.DisplayName}\nSonraki Gelir: {remainingDays} gün sonra\nDöngü Geliri: {activeProject.CycleRevenue.Amount:N0} | Döngü Kârý: {activeProject.CycleProfit.Amount:N0}\nÇalýþanlar: {employeeNames}");
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 112f);
            button.onClick.AddListener(() => ShowActiveProjectEditor(selectedSector, activeProject));
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
            var label = $"{project.DisplayName}\nAktif: {activeCount} | Döngü Kârý: {GetCycleProfit(resultPreview).Amount:N0}";
            var button = CreateButton(contentRoot, $"Project_{project.Id}", label);
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 84f);
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
            var button = CreateButton(contentRoot, $"Template_{project.Id}", $"{project.DisplayName}\nDurum: {selectedText}");
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 72f);
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

                CreateInfoCard($"{role.DisplayName} için {assignment.Count} çalýþan seçmelisin.", 64f);

                for (var slotIndex = 0; slotIndex < assignment.Count; slotIndex++)
                {
                    CreateEmployeeSlotEditor(role, assignmentIndex, slotIndex);
                }
            }
        }

        private void CreateEmployeeSlotEditor(CompanySimulator.Features.Employees.Runtime.Definitions.EmployeeRoleDefinition role, int assignmentIndex, int slotIndex)
        {
            var slotId = BuildEmployeeSlotId(role, assignmentIndex, slotIndex);
            draftEmployeeSelections.TryGetValue(slotId, out var selectedEmployee);
            var label = selectedEmployee == null
                ? $"{role.DisplayName} / Slot {slotIndex + 1}\nBoþ çalýþan seç"
                : $"{role.DisplayName} / Slot {slotIndex + 1}\n{selectedEmployee.DisplayName} | Kademe: {selectedEmployee.QualityTier} | x{selectedEmployee.IncomeMultiplier:0.0}";

            var slotButton = CreateButton(contentRoot, $"EmployeeSlot_{slotId}", label);
            slotButton.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 82f);
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
                CreateInfoCard($"{role.DisplayName} için boþta çalýþan bulunmuyor.", 60f);
            }
            else
            {
                for (var i = 0; i < availableEmployees.Count; i++)
                {
                    CreateEmployeeCandidateButton(slotId, availableEmployees[i]);
                }
            }

            if (selectedEmployee != null)
            {
                var clearButton = CreateButton(contentRoot, $"Clear_{slotId}", "Atamayý Temizle");
                clearButton.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 54f);
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

        private void CreateEmployeeCandidateButton(string slotId, EmployeeRuntimeData employee)
        {
            var label = $"{employee.DisplayName}\nKademe: {employee.QualityTier} | Çarpan: x{employee.IncomeMultiplier:0.0} | Maaþ: {employee.ExpectedDailySalary.Amount:N0}";
            var button = CreateButton(contentRoot, $"Candidate_{slotId}_{employee.Id}", label);
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 82f);
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

                CreateInvestmentEditor(project, investment);
                hasVisibleInvestment = true;
            }

            if (!hasVisibleInvestment && rentInvestment == null && purchaseInvestment == null)
            {
                CreateInfoCard("Bu iþ için yatýrým tanýmý bulunmuyor.");
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
            row.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 56f);

            CreatePropertyChoiceButton(row.transform, rentInvestment, "Kirala");
            CreatePropertyChoiceButton(row.transform, purchaseInvestment, "Satýn Al");
        }

        private void CreatePropertyChoiceButton(Transform parent, InvestmentTypeDefinition investment, string label)
        {
            var button = CreateButton(parent, "PropertyChoice_" + investment.Id, selectedPropertyInvestment == investment ? label + " (Seçili)" : label);
            var layoutElement = button.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 180f;
            layoutElement.preferredHeight = 56f;
            button.onClick.AddListener(() =>
            {
                selectedPropertyInvestment = investment;
                RefreshDraftPage();
            });
        }

        private void CreateInvestmentEditor(ProjectExecutionDefinition project, InvestmentTypeDefinition investment)
        {
            if (!draftBudgetCache.ContainsKey(investment))
            {
                draftBudgetCache[investment] = GetDefaultBudget(project, investment);
            }

            var card = CreateUiObject($"Investment_{investment.Id}", contentRoot);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(0f, 116f);

            var cardImage = card.AddComponent<Image>();
            cardImage.color = new Color(0.18f, 0.2f, 0.25f, 1f);

            var nameText = CreateText(card.transform, investment.DisplayName, 22, TextAnchor.UpperLeft);
            StretchToParent(nameText.rectTransform, 14f, 10f, 220f, 54f);

            var typeLabel = investment.IsRecurringExpense ? "Gelirden Düþer" : "Peþin";
            var minimumBudget = GetMinimumAllowedBudget(project, investment);
            var maximumBudget = GetMaximumAllowedBudget(investment);
            var detailText = CreateText(
                card.transform,
                selectedActiveProject == null
                    ? $"Gider Tipi: {typeLabel}\nMinimum: {investment.MinimumBudget:N0} | Önerilen: {investment.RecommendedBudget:N0} | Maksimum: {maximumBudget:N0}"
                    : $"Gider Tipi: {typeLabel}\nMevcut: {minimumBudget:N0} | Maksimum: {maximumBudget:N0} | Sadece artýþ yapýlabilir",
                18,
                TextAnchor.UpperLeft);
            StretchToParent(detailText.rectTransform, 14f, 42f, 220f, 10f);

            var inputField = CreateInputField(card.transform, draftBudgetCache[investment].ToString());
            var inputRect = inputField.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(1f, 0.5f);
            inputRect.anchorMax = new Vector2(1f, 0.5f);
            inputRect.pivot = new Vector2(1f, 0.5f);
            inputRect.anchoredPosition = new Vector2(-16f, 6f);
            inputRect.sizeDelta = new Vector2(180f, 40f);

            var evaluationText = CreateText(card.transform, string.Empty, 18, TextAnchor.MiddleRight);
            var evaluationRect = evaluationText.rectTransform;
            evaluationRect.anchorMin = new Vector2(1f, 0f);
            evaluationRect.anchorMax = new Vector2(1f, 0f);
            evaluationRect.pivot = new Vector2(1f, 0f);
            evaluationRect.anchoredPosition = new Vector2(-16f, 12f);
            evaluationRect.sizeDelta = new Vector2(220f, 30f);

            void RefreshEvaluation(string inputValue)
            {
                var parsedBudget = ParseBudget(inputValue, minimumBudget, maximumBudget);
                draftBudgetCache[investment] = parsedBudget;
                inputField.text = parsedBudget.ToString();
                evaluationText.text = $"Deðerlendirme: {investment.GetBudgetEvaluationLabel(parsedBudget)}";
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
            draftResultText.text = BuildResultSummary(result);
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
                draftResultText.text = "Ýþ baþlatýlamadý. Muhtemelen bakiye yetersiz.\n\n" + BuildResultSummary(economyManager.PreviewProject(request));
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

        private string BuildResultSummary(ProjectEconomyResult result)
        {
            return $"Tahmini Gelir: {result.Revenue.Amount:N0}";
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
            var titleText = CreateText(contentRoot, title, 24, TextAnchor.MiddleLeft);
            titleText.rectTransform.sizeDelta = new Vector2(0f, 36f);
            titleText.color = new Color(0.94f, 0.94f, 0.98f, 1f);
        }

        private Text CreateInfoCard(string message, float height = 58f)
        {
            return RuntimePanelUiUtility.CreateInfoCard(contentRoot, defaultFont, message, height);
        }

        private GameObject CreateUiObject(string objectName, Transform parent)
        {
            return RuntimePanelUiUtility.CreateUiObject(objectName, parent);
        }

        private Button CreateButton(Transform parent, string objectName, string label)
        {
            return RuntimePanelUiUtility.CreateButton(parent, defaultFont, objectName, label);
        }

        private InputField CreateInputField(Transform parent, string initialValue)
        {
            return RuntimePanelUiUtility.CreateInputField(parent, defaultFont, initialValue);
        }

        private Text CreateText(Transform parent, string value, int fontSize, TextAnchor anchor)
        {
            return RuntimePanelUiUtility.CreateText(parent, defaultFont, value, fontSize, anchor);
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
