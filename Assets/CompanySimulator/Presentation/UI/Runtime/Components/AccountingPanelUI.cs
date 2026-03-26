using CompanySimulator.Features.Accounting.Runtime.Components;
using CompanySimulator.Features.Accounting.Runtime.Models;
using CompanySimulator.Features.Employees.Runtime.Models;
using CompanySimulator.Presentation.UI.Runtime.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CompanySimulator.Presentation.UI.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class AccountingPanelUI : MonoBehaviour
    {
        [SerializeField] private CompanyAccountingManager companyAccountingManager;
        [SerializeField] private SectorPanelUI sectorPanelUI;
        [SerializeField] private EmployeePanelUI employeePanelUI;
        [SerializeField] private BankPanelUI bankPanelUI;
        [SerializeField] private FinanceOverviewPanelUI financeOverviewPanelUI;
        [SerializeField] private RivalCompanyPanelUI rivalCompanyPanelUI;
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private Vector2 panelSize = new Vector2(720f, 700f);

        private Font defaultFont;
        private GameObject panelRoot;
        private RectTransform contentRoot;
        private Text pageTitleText;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            companyAccountingManager ??= FindObjectOfType<CompanyAccountingManager>();
            if (companyAccountingManager == null)
            {
                companyAccountingManager = new GameObject("CompanyAccountingManager", typeof(CompanyAccountingManager)).GetComponent<CompanyAccountingManager>();
            }

            sectorPanelUI ??= FindObjectOfType<SectorPanelUI>();
            employeePanelUI ??= FindObjectOfType<EmployeePanelUI>();
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
            if (companyAccountingManager != null)
            {
                companyAccountingManager.DataChanged -= RefreshPage;
                companyAccountingManager.DataChanged += RefreshPage;
            }

            RefreshPage();
        }

        private void OnDisable()
        {
            if (companyAccountingManager != null)
            {
                companyAccountingManager.DataChanged -= RefreshPage;
            }
        }

        public void OpenPanel()
        {
            if (sectorPanelUI != null && sectorPanelUI.IsOpen)
            {
                sectorPanelUI.ClosePanel();
            }

            if (employeePanelUI != null && employeePanelUI.IsOpen)
            {
                employeePanelUI.ClosePanel();
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

            panelRoot.SetActive(true);
            RefreshPage();
        }

        public void ClosePanel()
        {
            panelRoot.SetActive(false);
        }

        private void RefreshPage()
        {
            if (contentRoot == null || companyAccountingManager == null)
            {
                return;
            }

            if (!companyAccountingManager.IsInitialized)
            {
                companyAccountingManager.Initialize();
            }

            pageTitleText.text = "Muhasebe";
            RuntimePanelUiUtility.ClearChildren(contentRoot);

            var snapshot = companyAccountingManager.GetCurrentCycleSnapshot();
            CreateInfoCard($"Aktif İş / Kapasite: {snapshot.ActiveProjectCount} / {snapshot.MaxActiveProjectCount}", 62f);
            CreateInfoCard($"Vergiye Kalan Gün: {snapshot.DaysUntilTaxPayment}\nTahmini Vergi: {snapshot.EstimatedTax.Amount:N0}\nSon Vergi Ödemesi: {snapshot.LastTaxPayment.Amount:N0}", 98f);
            CreateInfoCard($"Döngü Geliri: {snapshot.Income.Amount:N0}\nDöngü Gideri: {snapshot.Expenses.Amount:N0}\nDöngü Kârı: {snapshot.Profit.Amount:N0}", 94f);

            if (companyAccountingManager.AccountantRole == null)
            {
                CreateInfoCard("Muhasebeçi rolü bulunamadı. Rol kimliği veya görünen adı 'muhasebeci' olmalı.", 76f);
                return;
            }

            CreateSectionTitle("Atanmış Muhasebeciler");
            var assignedAccountants = companyAccountingManager.GetAssignedAccountants();
            if (assignedAccountants.Count == 0)
            {
                CreateInfoCard("Şirkete atanmış muhasebeçi yok. Muhasebeçi olmadan yeni iş başlatılamaz.", 76f);
            }
            else
            {
                for (var i = 0; i < assignedAccountants.Count; i++)
                {
                    CreateAssignedAccountantCard(assignedAccountants[i]);
                }
            }

            CreateSectionTitle("Boştaki Muhasebeciler");
            var availableAccountants = companyAccountingManager.GetAvailableAccountants();
            if (availableAccountants.Count == 0)
            {
                CreateInfoCard("Şu anda şirkete atanabilecek boşta muhasebeçi yok.", 66f);
            }
            else
            {
                for (var i = 0; i < availableAccountants.Count; i++)
                {
                    CreateAvailableAccountantCard(availableAccountants[i]);
                }
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

        private void BuildUi()
        {
            CreateOpenButton();
            CreatePanel();
            panelRoot.SetActive(false);
        }

        private void CreateOpenButton()
        {
            var button = CreateButton(rootCanvas.transform, "AccountingOpenButton", "Muhasebe");
            var buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 1f);
            buttonRect.anchorMax = new Vector2(0f, 1f);
            buttonRect.pivot = new Vector2(0f, 1f);
            buttonRect.anchoredPosition = new Vector2(420f, -80f);
            buttonRect.sizeDelta = new Vector2(180f, 44f);
            button.onClick.AddListener(OpenPanel);
        }

        private void CreatePanel()
        {
            panelRoot = CreateUiObject("AccountingPanel", rootCanvas.transform);
            var panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(0f, -10f);
            panelRect.sizeDelta = panelSize;

            panelRoot.AddComponent<Image>().color = new Color(0.1f, 0.12f, 0.16f, 0.98f);

            var headerRoot = CreateUiObject("Header", panelRoot.transform);
            var headerRect = headerRoot.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0f, 70f);
            headerRoot.AddComponent<Image>().color = new Color(0.17f, 0.21f, 0.29f, 1f);

            pageTitleText = CreateText(headerRoot.transform, "Muhasebe", 28, TextAnchor.MiddleLeft);
            RuntimePanelUiUtility.StretchToParent(pageTitleText.rectTransform, 18f, 8f, 140f, 8f);

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
        }

        private void CreateAssignedAccountantCard(EmployeeRuntimeData accountant)
        {
            var contribution = Mathf.Max(1, Mathf.CeilToInt(accountant.IncomeMultiplier));
            CreateInfoCard($"{accountant.DisplayName}\nKademe: {accountant.QualityTier} | Kapasite Katkısı: +{contribution}\nGünlük Maaş: {accountant.ExpectedDailySalary.Amount:N0}", 92f);

            var canUnassign = companyAccountingManager.CanUnassignAccountant(accountant, out var validationMessage);
            var button = CreateButton(contentRoot, $"Unassign_{accountant.Id}", canUnassign ? "Şirketten Ayır" : "Aktif işler yüzünden ayrılamaz");
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 54f);
            button.interactable = canUnassign;
            button.onClick.AddListener(() =>
            {
                if (companyAccountingManager.TryUnassignAccountant(accountant, out _))
                {
                    RefreshPage();
                }
            });

            if (!canUnassign && !string.IsNullOrWhiteSpace(validationMessage))
            {
                CreateInfoCard(validationMessage, 66f);
            }
        }

        private void CreateAvailableAccountantCard(EmployeeRuntimeData accountant)
        {
            var contribution = Mathf.Max(1, Mathf.CeilToInt(accountant.IncomeMultiplier));
            CreateInfoCard($"{accountant.DisplayName}\nKademe: {accountant.QualityTier} | Kapasite Katkısı: +{contribution}\nGünlük Maaş: {accountant.ExpectedDailySalary.Amount:N0}", 92f);

            var button = CreateButton(contentRoot, $"Assign_{accountant.Id}", "Şirkete Ata");
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 54f);
            button.onClick.AddListener(() =>
            {
                if (companyAccountingManager.TryAssignAccountant(accountant, out _))
                {
                    RefreshPage();
                }
            });
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
