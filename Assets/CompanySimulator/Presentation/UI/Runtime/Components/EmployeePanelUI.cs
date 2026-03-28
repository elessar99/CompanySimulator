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
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private Vector2 panelSize = new Vector2(700f, 680f);

        private Font defaultFont;
        private GameObject panelRoot;
        private RectTransform contentRoot;
        private Text pageTitleText;
        private Button backButton;
        private EmployeeRoleDefinition selectedRole;
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
            EnsureCanvas();
            EnsureEventSystem();
            defaultFont = LoadDefaultFont();
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

            panelRoot.SetActive(true);
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
            ClearChildren();

            var employees = employeeManager.GetEmployeesByRole(role);
            EmployeeRoleEmployeesPage.Render(employees, (message, height) => CreateInfoCard(message, height), CreateEmployeeCard);

            var applicationsButton = CreateButton(contentRoot, "ApplicationsButton", "İş Başvuruları");
            applicationsButton.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 62f);
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
            ClearChildren();

            var applicants = employeeManager.GetApplicantsByRole(role);
            EmployeeApplicationsPage.Render(applicants, message => CreateInfoCard(message), CreateApplicantButton);
        }

        private void CreateRoleButton(EmployeeRoleDefinition role)
        {
            var employeeCount = employeeManager.GetEmployeeCount(role);
            var applicantCount = employeeManager.GetApplicantCount(role);
            var button = CreateButton(contentRoot, $"Role_{role.Id}", $"{role.DisplayName}\nÇalışan: {employeeCount} | Başvuru: {applicantCount}");
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 78f);
            button.onClick.AddListener(() => ShowEmployeesForRole(role));
        }

        private void CreateApplicantButton(EmployeeRuntimeData applicant)
        {
            var button = CreateButton(contentRoot, $"Applicant_{applicant.Id}", $"{applicant.DisplayName}\nKademe: {applicant.QualityTier} | Katkı Çarpanı: x{applicant.IncomeMultiplier:0.0}\nİstenen Maaş: {applicant.ExpectedDailySalary.Amount:N0}\nİşe Al");
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 108f);
            button.onClick.AddListener(() =>
            {
                employeeManager.TryHireApplicant(applicant);
                ShowApplicationsForRole(selectedRole);
            });
        }

        private void CreateEmployeeCard(EmployeeRuntimeData employee)
        {
            var durum = employee.IsAssigned ? $"Durum: Çalışıyor\nGörev: {employee.CurrentAssignmentName}" : "Durum: Boşta";
            CreateInfoCard($"{employee.DisplayName}\nKademe: {employee.QualityTier} | Katkı Çarpanı: x{employee.IncomeMultiplier:0.0}\nGünlük Maaş: {employee.ExpectedDailySalary.Amount:N0}\n{durum}", 96f);

            var canFire = employeeManager != null && employeeManager.CanFireEmployee(employee);
            var fireButton = CreateButton(contentRoot, $"Fire_{employee.Id}", canFire ? "Çalışanı Kov" : "Çalışıyor - Kovulamaz");
            fireButton.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 54f);
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
            var button = CreateButton(rootCanvas.transform, "EmployeesOpenButton", "Çalışanlar");
            var buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 1f);
            buttonRect.anchorMax = new Vector2(0f, 1f);
            buttonRect.pivot = new Vector2(0f, 1f);
            buttonRect.anchoredPosition = new Vector2(220f, -80f);
            buttonRect.sizeDelta = new Vector2(180f, 44f);
            button.onClick.AddListener(OpenPanel);
        }

        private void CreatePanel()
        {
            panelRoot = CreateUiObject("EmployeePanel", rootCanvas.transform);
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
            headerRoot.AddComponent<Image>().color = new Color(0.17f, 0.21f, 0.29f, 1f);

            pageTitleText = CreateText(headerRoot.transform, "Çalışanlar", 28, TextAnchor.MiddleLeft);
            StretchToParent(pageTitleText.rectTransform, 18f, 8f, 140f, 8f);

            backButton = CreateButton(headerRoot.transform, "BackButton", "←");
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
            scrollRoot.AddComponent<Image>().color = new Color(0.13f, 0.15f, 0.19f, 0.92f);

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

            UpdateHeaderButtons();
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
