using CompanySimulator.Features.Employees.Runtime.Components;
using CompanySimulator.Features.Employees.Runtime.Definitions;
using CompanySimulator.Features.Employees.Runtime.Models;
using CompanySimulator.Presentation.UI.Runtime.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CompanySimulator.Presentation.UI.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class EmployeePanelUI : MonoBehaviour
    {
        [SerializeField] private EmployeeManager employeeManager;
        [SerializeField] private SectorPanelUI sectorPanelUI;
        [SerializeField] private AccountingPanelUI accountingPanelUI;
        [SerializeField] private BankPanelUI bankPanelUI;
        [SerializeField] private FinanceOverviewPanelUI financeOverviewPanelUI;
        [SerializeField] private RivalCompanyPanelUI rivalCompanyPanelUI;
        [SerializeField] private DebugPanelUI debugPanelUI;
        [SerializeField] private SecurityPanelUI securityPanelUI;
        [SerializeField] private ShopPanelUI shopPanelUI;
        [SerializeField] private InventoryPanelUI inventoryPanelUI;
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private Sprite appIcon;
        [SerializeField] private Vector2 panelSize = new Vector2(980f, 720f);
        [SerializeField] private float panelVerticalOffset = 72f;

        private static readonly Color ColBg = new Color(0.035f, 0.067f, 0.122f, 1f);
        private static readonly Color ColPanel = new Color(0.063f, 0.098f, 0.169f, 1f);
        private static readonly Color ColSurface = new Color(0.082f, 0.125f, 0.204f, 1f);
        private static readonly Color ColSurfaceAlt = new Color(0.047f, 0.078f, 0.141f, 1f);
        private static readonly Color ColText = new Color(0.933f, 0.957f, 1f, 1f);
        private static readonly Color ColMuted = new Color(0.561f, 0.639f, 0.784f, 1f);
        private static readonly Color ColGrey = new Color(0.47f, 0.52f, 0.6f, 1f);
        private static readonly Color ColBlue = new Color(0.353f, 0.627f, 1f, 1f);
        private static readonly Color ColCyan = new Color(0.302f, 0.886f, 0.816f, 1f);
        private static readonly Color ColGold = new Color(0.961f, 0.769f, 0.365f, 1f);
        private static readonly Color ColGreen = new Color(0.263f, 0.839f, 0.561f, 1f);
        private static readonly Color ColRed = new Color(1f, 0.42f, 0.506f, 1f);
        private static readonly Color ColPurple = new Color(0.62f, 0.46f, 1f, 1f);

        private Font defaultFont;
        private Sprite roundedSprite;
        private GameObject panelRoot;
        private RectTransform contentRoot;
        private Text pageTitleText;
        private Button backButton;
        private EmployeeRoleDefinition selectedRole;
        private Transform roleGridParent;
        private Transform employeeGridParent;
        private Transform applicantGridParent;
        private EmployeePageState currentPage;

        private void Awake()
        {
            employeeManager ??= FindObjectOfType<EmployeeManager>();
            sectorPanelUI ??= FindObjectOfType<SectorPanelUI>();
            accountingPanelUI ??= FindObjectOfType<AccountingPanelUI>();
            bankPanelUI ??= FindObjectOfType<BankPanelUI>();
            financeOverviewPanelUI ??= FindObjectOfType<FinanceOverviewPanelUI>();
            rivalCompanyPanelUI ??= FindObjectOfType<RivalCompanyPanelUI>();
            debugPanelUI ??= FindObjectOfType<DebugPanelUI>();
            securityPanelUI ??= FindObjectOfType<SecurityPanelUI>();
            shopPanelUI ??= FindObjectOfType<ShopPanelUI>();
            inventoryPanelUI ??= FindObjectOfType<InventoryPanelUI>();
            EnsureCanvas();
            EnsureEventSystem();
            defaultFont = LoadDefaultFont();
            roundedSprite = LoadRoundedSprite();
            BuildUi();
        }

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void OnEnable()
        {
            if (employeeManager != null)
            {
                employeeManager.DataChanged -= HandleDataChanged;
                employeeManager.DataChanged += HandleDataChanged;
            }

            RefreshPage();
        }

        private void OnDisable()
        {
            if (employeeManager != null)
            {
                employeeManager.DataChanged -= HandleDataChanged;
            }
        }

        public void OpenPanel()
        {
            RuntimePanelUiUtility.SetComputerPanelActive(rootCanvas, true);

            if (sectorPanelUI != null && sectorPanelUI.IsOpen)
            {
                sectorPanelUI.ClosePanel();
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

            if (shopPanelUI != null && shopPanelUI.IsOpen)
            {
                shopPanelUI.ClosePanel();
            }

            if (inventoryPanelUI != null && inventoryPanelUI.IsOpen)
            {
                inventoryPanelUI.ClosePanel();
            }

            panelRoot.SetActive(true);
            RuntimePanelUiUtility.BringToFront(panelRoot);
            NavigateToRoleList();
        }

        public void ClosePanel()
        {
            NavigateToRoleList();
            panelRoot.SetActive(false);
        }

        private void GoBack()
        {
            switch (currentPage)
            {
                case EmployeePageState.Applications:
                    ShowEmployeesForRole(selectedRole);
                    return;
                case EmployeePageState.RoleEmployees:
                    NavigateToRoleList();
                    return;
                default:
                    return;
            }
        }

        private void NavigateToRoleList()
        {
            selectedRole = null;
            currentPage = EmployeePageState.RoleList;
            UpdateHeaderButtons();
            ShowRoleList();
        }

        private void HandleDataChanged()
        {
            RefreshPage();
        }

        private void RefreshPage()
        {
            if (employeeManager == null)
            {
                return;
            }

            if (!employeeManager.IsInitialized)
            {
                employeeManager.Initialize();
            }

            switch (currentPage)
            {
                case EmployeePageState.RoleEmployees:
                    ShowEmployeesForRole(selectedRole);
                    break;
                case EmployeePageState.Applications:
                    ShowApplicationsForRole(selectedRole);
                    break;
                default:
                    ShowRoleList();
                    break;
            }
        }

        private void ShowRoleList()
        {
            currentPage = EmployeePageState.RoleList;
            UpdateHeaderButtons();
            pageTitleText.text = "Çalışanlar";
            roleGridParent = null;
            employeeGridParent = null;
            applicantGridParent = null;
            ClearChildren();

            EmployeeRoleListPage.Render(employeeManager, message => CreateInfoCard(message), CreateRoleButton);
        }

        private void ShowEmployeesForRole(EmployeeRoleDefinition role)
        {
            if (role == null)
            {
                NavigateToRoleList();
                return;
            }

            currentPage = EmployeePageState.RoleEmployees;
            selectedRole = role;
            UpdateHeaderButtons();
            pageTitleText.text = role.DisplayName;
            roleGridParent = null;
            employeeGridParent = null;
            applicantGridParent = null;
            ClearChildren();

            var employees = employeeManager.GetEmployeesByRole(role);
            EmployeeRoleEmployeesPage.Render(employees, (message, height) => CreateInfoCard(message, height), CreateEmployeeCard);

            var applicationsButton = CreateStyledButton(contentRoot, "ApplicationsButton", "İş Başvuruları", ColBlue, Blend(ColBlue, ColCyan, 0.28f), Darken(ColBlue, 0.22f), ColText, TextAnchor.MiddleCenter);
            var applicationsLayout = applicationsButton.gameObject.AddComponent<LayoutElement>();
            applicationsLayout.preferredHeight = 52f;
            applicationsLayout.minHeight = 52f;
            applicationsButton.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 52f);
            applicationsButton.onClick.AddListener(() => ShowApplicationsForRole(role));
        }

        private void ShowApplicationsForRole(EmployeeRoleDefinition role)
        {
            if (role == null)
            {
                NavigateToRoleList();
                return;
            }

            currentPage = EmployeePageState.Applications;
            selectedRole = role;
            UpdateHeaderButtons();
            pageTitleText.text = role.DisplayName + " / İş Başvuruları";
            roleGridParent = null;
            employeeGridParent = null;
            applicantGridParent = null;
            ClearChildren();

            var applicants = employeeManager.GetApplicantsByRole(role);
            EmployeeApplicationsPage.Render(applicants, message => CreateInfoCard(message), CreateApplicantButton);
        }

        private void CreateRoleButton(EmployeeRoleDefinition role)
        {
            var employeeCount = employeeManager.GetEmployeeCount(role);
            var applicantCount = employeeManager.GetApplicantCount(role);
            var accent = GetRoleAccent(role);
            var card = CreateSurface(EnsureRoleGridHost(), $"Role_{role.Id}", 186f, ColPanel);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(380f, 186f);
            var cardLayout = card.GetComponent<LayoutElement>();
            cardLayout.preferredWidth = 380f;
            cardLayout.minWidth = 380f;
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

            var topRow = CreateUiObject("TopRow", content.transform);
            topRow.AddComponent<LayoutElement>().preferredHeight = 30f;
            var topLayout = topRow.AddComponent<HorizontalLayoutGroup>();
            topLayout.spacing = 8f;
            topLayout.childControlWidth = false;
            topLayout.childControlHeight = true;
            topLayout.childForceExpandWidth = false;
            topLayout.childForceExpandHeight = false;
            topLayout.childAlignment = TextAnchor.MiddleLeft;

            CreateTag(topRow.transform, employeeCount + " Çalışan", new Color(accent.r, accent.g, accent.b, 0.18f), accent, 13);
            CreateTag(topRow.transform, applicantCount + " Başvuru", new Color(ColBlue.r, ColBlue.g, ColBlue.b, 0.16f), ColBlue, 13);

            var title = CreateText(content.transform, role.DisplayName, 22, TextAnchor.MiddleLeft);
            title.color = ColText;
            title.fontStyle = FontStyle.Bold;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

            var subtitle = CreateText(content.transform, "Çalışanları ve başvuruları yönet.", 14, TextAnchor.MiddleLeft);
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

            CreateMiniStat(statRow.transform, employeeCount.ToString(), "Kadrodaki");
            CreateMiniStat(statRow.transform, applicantCount.ToString(), "Bekleyen");
            button.onClick.AddListener(() => ShowEmployeesForRole(role));
        }

        private void CreateApplicantButton(EmployeeRuntimeData applicant)
        {
            var accent = GetEmployeeAccent(applicant);
            var cardColor = Blend(ColPanel, accent, 0.12f);
            var card = CreateSurface(EnsureApplicantGridHost(), $"Applicant_{applicant.Id}", 196f, cardColor);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(380f, 196f);
            var cardLayout = card.GetComponent<LayoutElement>();
            cardLayout.preferredWidth = 380f;
            cardLayout.minWidth = 380f;
            AddHoverEffect(card, cardColor, Blend(cardColor, accent, 0.18f));
            CreateAccentBar(card.transform, accent);

            var content = CreateStretchContainer(card.transform, "Content", 12f, 12f, 12f, 12f);
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 0, 0, 0);
            contentLayout.spacing = 8f;
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
            topLayout.childForceExpandWidth = false;
            topLayout.childForceExpandHeight = false;
            topLayout.childAlignment = TextAnchor.MiddleLeft;

            var name = CreateText(topRow.transform, applicant.DisplayName, 18, TextAnchor.MiddleLeft);
            name.color = ColText;
            name.fontStyle = FontStyle.Bold;
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            CreateTag(topRow.transform, RuntimePanelUiUtility.GetEmployeeQualityLabel(applicant.QualityTier), new Color(accent.r, accent.g, accent.b, 0.18f), accent, 13);

            var role = CreateText(content.transform, applicant.Role != null ? applicant.Role.DisplayName : "Rol Yok", 14, TextAnchor.MiddleLeft);
            role.color = ColMuted;
            role.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

            var statRow = CreateUiObject("StatsRow", content.transform);
            statRow.AddComponent<LayoutElement>().preferredHeight = 42f;
            var statGrid = statRow.AddComponent<GridLayoutGroup>();
            statGrid.cellSize = new Vector2(116f, 42f);
            statGrid.spacing = new Vector2(6f, 0f);
            statGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            statGrid.constraintCount = 3;
            statGrid.childAlignment = TextAnchor.MiddleCenter;

            CreateMiniStat(statRow.transform, applicant.ExpectedDailySalary.Amount.ToString("N0"), "Günlük Maaş");
            CreateMiniStat(statRow.transform, "x" + applicant.IncomeMultiplier.ToString("0.0"), "Katkı");
            CreateMiniStat(statRow.transform, applicant.ApplicantRemainingDays + "g", "Kalan Süre");

            CreateFlexibleSpacer(content.transform);

            var button = CreateStyledButton(content.transform, $"Hire_{applicant.Id}", "İşe Al", ColBlue, Blend(ColBlue, ColCyan, 0.28f), Darken(ColBlue, 0.22f), ColText, TextAnchor.MiddleCenter);
            button.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;
            button.onClick.AddListener(() =>
            {
                employeeManager.TryHireApplicant(applicant);
                ShowApplicationsForRole(selectedRole);
            });
        }

        private void CreateEmployeeCard(EmployeeRuntimeData employee)
        {
            var accent = GetEmployeeAccent(employee);
            var cardColor = Blend(ColPanel, accent, 0.12f);
            var statusLabel = employee.IsAssigned ? "Çalışıyor" : "Boşta";
            var statusColor = employee.IsAssigned ? ColGold : ColGreen;
            var assignmentText = employee.IsAssigned && !string.IsNullOrWhiteSpace(employee.CurrentAssignmentName)
                ? "Görev: " + employee.CurrentAssignmentName
                : "Atama bekliyor";
            var card = CreateSurface(EnsureEmployeeGridHost(), $"Employee_{employee.Id}", 196f, cardColor);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(380f, 196f);
            var cardLayout = card.GetComponent<LayoutElement>();
            cardLayout.preferredWidth = 380f;
            cardLayout.minWidth = 380f;
            AddHoverEffect(card, cardColor, Blend(cardColor, accent, 0.18f));
            CreateAccentBar(card.transform, accent);

            var content = CreateStretchContainer(card.transform, "Content", 12f, 12f, 12f, 12f);
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 0, 0, 0);
            contentLayout.spacing = 8f;
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
            topLayout.childForceExpandWidth = false;
            topLayout.childForceExpandHeight = false;
            topLayout.childAlignment = TextAnchor.MiddleLeft;

            var name = CreateText(topRow.transform, employee.DisplayName, 18, TextAnchor.MiddleLeft);
            name.color = ColText;
            name.fontStyle = FontStyle.Bold;
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            CreateTag(topRow.transform, RuntimePanelUiUtility.GetEmployeeQualityLabel(employee.QualityTier), new Color(accent.r, accent.g, accent.b, 0.18f), accent, 13);
            CreateTag(topRow.transform, statusLabel, new Color(statusColor.r, statusColor.g, statusColor.b, 0.18f), statusColor, 13);

            var assignment = CreateText(content.transform, assignmentText, 14, TextAnchor.MiddleLeft);
            assignment.color = ColMuted;
            assignment.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

            var statRow = CreateUiObject("StatsRow", content.transform);
            statRow.AddComponent<LayoutElement>().preferredHeight = 42f;
            var statGrid = statRow.AddComponent<GridLayoutGroup>();
            statGrid.cellSize = new Vector2(174f, 42f);
            statGrid.spacing = new Vector2(8f, 0f);
            statGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            statGrid.constraintCount = 2;
            statGrid.childAlignment = TextAnchor.MiddleCenter;

            CreateMiniStat(statRow.transform, employee.ExpectedDailySalary.Amount.ToString("N0"), "Günlük Maaş");
            CreateMiniStat(statRow.transform, "x" + employee.IncomeMultiplier.ToString("0.0"), "Katkı");

            CreateFlexibleSpacer(content.transform);

            var canFire = employeeManager != null && employeeManager.CanFireEmployee(employee);
            var fireButton = CreateStyledButton(
                content.transform,
                $"Fire_{employee.Id}",
                canFire ? "Çalışanı Kov" : "Çalışıyor - Kovulamaz",
                canFire ? new Color(ColRed.r, ColRed.g, ColRed.b, 0.16f) : ColSurfaceAlt,
                canFire ? new Color(ColRed.r, ColRed.g, ColRed.b, 0.26f) : Blend(ColSurfaceAlt, ColBlue, 0.1f),
                canFire ? new Color(ColRed.r, ColRed.g, ColRed.b, 0.34f) : Darken(ColSurfaceAlt, 0.08f),
                canFire ? ColRed : ColMuted,
                TextAnchor.MiddleCenter);
            fireButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;
            fireButton.interactable = canFire;
            fireButton.onClick.AddListener(() =>
            {
                if (employeeManager != null && employeeManager.TryFireEmployee(employee))
                {
                    ShowEmployeesForRole(selectedRole);
                }
            });
        }

        private void EnsureCanvas()
        {
            if (rootCanvas == null)
            {
                rootCanvas = FindObjectOfType<Canvas>();
            }

            if (rootCanvas == null)
            {
                var canvasObject = new GameObject("MainCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                rootCanvas = canvasObject.GetComponent<Canvas>();
                rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            RuntimePanelUiUtility.EnsureResponsiveCanvasScaler(rootCanvas);
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
            var button = RuntimePanelUiUtility.CreateDesktopAppButton(RuntimePanelUiUtility.GetOrCreateComputerDesktopIconRoot(rootCanvas), defaultFont, "EmployeesOpenButton", "Çalışanlar", appIcon, "EMP", ColGreen);
            button.onClick.AddListener(OpenPanel);
        }

        private void CreatePanel()
        {
            panelRoot = CreateUiObject("EmployeePanel", RuntimePanelUiUtility.GetOrCreateComputerWindowRoot(rootCanvas));
            var panelRect = panelRoot.GetComponent<RectTransform>();
            RuntimePanelUiUtility.ConfigureFillComputerPanelChild(panelRect, rootCanvas);
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
            var badgeText = CreateText(badge.transform, "EMP", 16, TextAnchor.MiddleCenter);
            badgeText.color = ColCyan;
            badgeText.fontStyle = FontStyle.Bold;
            StretchToParent(badgeText.rectTransform, 0f, 0f, 0f, 0f);

            pageTitleText = CreateText(headerRoot.transform, "Çalışanlar", 28, TextAnchor.MiddleLeft);
            pageTitleText.color = ColText;
            pageTitleText.fontStyle = FontStyle.Bold;
            pageTitleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            pageTitleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            pageTitleText.rectTransform.offsetMin = new Vector2(86f, -50f);
            pageTitleText.rectTransform.offsetMax = new Vector2(-140f, -14f);

            backButton = CreateStyledButton(headerRoot.transform, "BackButton", "←", ColSurfaceAlt, Blend(ColSurfaceAlt, ColBlue, 0.18f), Darken(ColSurfaceAlt, 0.15f), ColText, TextAnchor.MiddleCenter);
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
            ApplyRoundedImage(scrollRoot, new Color(ColPanel.r, ColPanel.g, ColPanel.b, 0.98f));
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
            layout.spacing = 36f;
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

        private Text CreateInfoCard(string message, float height = 58f)
        {
            var card = CreateSurface(contentRoot, "InfoCard", height, ColSurface);
            var text = CreateText(card.transform, message, 18, TextAnchor.MiddleLeft);
            text.color = ColMuted;
            StretchToParent(text.rectTransform, 14f, 8f, 14f, 8f);
            return text;
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

        private Transform EnsureRoleGridHost()
        {
            if (roleGridParent != null)
            {
                return roleGridParent;
            }

            var roleGridHost = CreateGridHost("RoleGrid", 380f, 186f);
            var roleGrid = roleGridHost.GetComponent<GridLayoutGroup>();
            if (roleGrid != null)
            {
                roleGrid.childAlignment = TextAnchor.UpperCenter;
            }

            roleGridParent = roleGridHost.transform;
            return roleGridParent;
        }

        private Transform EnsureEmployeeGridHost()
        {
            if (employeeGridParent != null)
            {
                return employeeGridParent;
            }

            var employeeGridHost = CreateGridHost("EmployeeGrid", 380f, 196f);
            var employeeGrid = employeeGridHost.GetComponent<GridLayoutGroup>();
            if (employeeGrid != null)
            {
                employeeGrid.childAlignment = TextAnchor.UpperCenter;
            }

            employeeGridParent = employeeGridHost.transform;
            return employeeGridParent;
        }

        private Transform EnsureApplicantGridHost()
        {
            if (applicantGridParent != null)
            {
                return applicantGridParent;
            }

            var applicantGridHost = CreateGridHost("ApplicantGrid", 380f, 196f);
            var applicantGrid = applicantGridHost.GetComponent<GridLayoutGroup>();
            if (applicantGrid != null)
            {
                applicantGrid.childAlignment = TextAnchor.UpperCenter;
            }

            applicantGridParent = applicantGridHost.transform;
            return applicantGridParent;
        }

        private GameObject CreateGridHost(string objectName, float cardWidth, float cardHeight)
        {
            const float gridSpacing = 36f;
            var host = CreateUiObject(objectName, contentRoot);
            var grid = host.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(cardWidth, cardHeight);
            grid.spacing = new Vector2(gridSpacing, gridSpacing);
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = CalculateGridColumnCount(cardWidth, gridSpacing);
            grid.childAlignment = TextAnchor.UpperLeft;

            var fitter = host.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return host;
        }

        private int CalculateGridColumnCount(float cardWidth, float spacing)
        {
            const float horizontalPadding = 80f;
            var referenceWidth = contentRoot != null && contentRoot.rect.width > 0f
                ? contentRoot.rect.width
                : panelRoot != null ? panelRoot.GetComponent<RectTransform>().rect.width : panelSize.x;
            var availableWidth = Mathf.Max(cardWidth, referenceWidth - horizontalPadding);
            return Mathf.Max(1, Mathf.FloorToInt((availableWidth + spacing) / (cardWidth + spacing)));
        }

        private Color GetRoleAccent(EmployeeRoleDefinition role)
        {
            var key = role != null ? role.Id ?? role.DisplayName ?? string.Empty : string.Empty;
            switch (Mathf.Abs(key.GetHashCode()) % 5)
            {
                case 0:
                    return ColBlue;
                case 1:
                    return ColCyan;
                case 2:
                    return ColGold;
                case 3:
                    return ColGreen;
                default:
                    return ColPurple;
            }
        }

        private Color GetEmployeeAccent(EmployeeRuntimeData employee)
        {
            if (employee == null)
            {
                return ColBlue;
            }

            switch (employee.QualityTier)
            {
                case EmployeeQualityTier.Kotu:
                    return ColGrey;
                case EmployeeQualityTier.Ortalama:
                    return ColGreen;
                case EmployeeQualityTier.Iyi:
                    return ColGold;
                case EmployeeQualityTier.Profesyonel:
                    return ColPurple;
                default:
                    return ColGrey;
            }
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

        private void StretchToParent(RectTransform rectTransform, float left, float bottom, float right, float top)
        {
            RuntimePanelUiUtility.StretchToParent(rectTransform, left, bottom, right, top);
        }

        private void UpdateHeaderButtons()
        {
            if (backButton != null)
            {
                backButton.gameObject.SetActive(currentPage != EmployeePageState.RoleList);
            }
        }

        private void ClearChildren()
        {
            RuntimePanelUiUtility.ClearChildren(contentRoot);
        }

        private enum EmployeePageState
        {
            RoleList = 0,
            RoleEmployees = 1,
            Applications = 2
        }
    }
}
