using CompanySimulator.Features.FinanceOverview.Runtime.Components;
using CompanySimulator.Features.FinanceOverview.Runtime.Models;
using CompanySimulator.Presentation.UI.Runtime.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CompanySimulator.Presentation.UI.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class FinanceOverviewPanelUI : MonoBehaviour
    {
        [SerializeField] private CompanyFinanceOverviewManager companyFinanceOverviewManager;
        [SerializeField] private SectorPanelUI sectorPanelUI;
        [SerializeField] private EmployeePanelUI employeePanelUI;
        [SerializeField] private AccountingPanelUI accountingPanelUI;
        [SerializeField] private BankPanelUI bankPanelUI;
        [SerializeField] private RivalCompanyPanelUI rivalCompanyPanelUI;
        [SerializeField] private DebugPanelUI debugPanelUI;
        [SerializeField] private SecurityPanelUI securityPanelUI;
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private Vector2 panelSize = new Vector2(760f, 720f);

        private Font defaultFont;
        private GameObject panelRoot;
        private RectTransform contentRoot;
        private Text pageTitleText;
        private FinanceOverviewTab currentTab;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            companyFinanceOverviewManager ??= FindObjectOfType<CompanyFinanceOverviewManager>();
            if (companyFinanceOverviewManager == null)
            {
                companyFinanceOverviewManager = new GameObject("CompanyFinanceOverviewManager", typeof(CompanyFinanceOverviewManager)).GetComponent<CompanyFinanceOverviewManager>();
            }

            sectorPanelUI ??= FindObjectOfType<SectorPanelUI>();
            employeePanelUI ??= FindObjectOfType<EmployeePanelUI>();
            accountingPanelUI ??= FindObjectOfType<AccountingPanelUI>();
            bankPanelUI ??= FindObjectOfType<BankPanelUI>();
            rivalCompanyPanelUI ??= FindObjectOfType<RivalCompanyPanelUI>();
            debugPanelUI ??= FindObjectOfType<DebugPanelUI>();
            securityPanelUI ??= FindObjectOfType<SecurityPanelUI>();
            EnsureCanvas();
            EnsureEventSystem();
            defaultFont = LoadDefaultFont();
            BuildUi();
        }

        private void OnEnable()
        {
            if (companyFinanceOverviewManager != null)
            {
                companyFinanceOverviewManager.DataChanged -= RefreshPage;
                companyFinanceOverviewManager.DataChanged += RefreshPage;
            }

            RefreshPage();
        }

        private void OnDisable()
        {
            if (companyFinanceOverviewManager != null)
            {
                companyFinanceOverviewManager.DataChanged -= RefreshPage;
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

            if (accountingPanelUI != null && accountingPanelUI.IsOpen)
            {
                accountingPanelUI.ClosePanel();
            }

            if (bankPanelUI != null && bankPanelUI.IsOpen)
            {
                bankPanelUI.ClosePanel();
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
            RefreshPage();
        }

        public void ClosePanel()
        {
            panelRoot.SetActive(false);
        }

        private void RefreshPage()
        {
            if (contentRoot == null || companyFinanceOverviewManager == null)
            {
                return;
            }

            if (!companyFinanceOverviewManager.IsInitialized)
            {
                companyFinanceOverviewManager.Initialize();
            }

            pageTitleText.text = "Finans Takibi";
            RuntimePanelUiUtility.ClearChildren(contentRoot);
            CreateTabRow();

            switch (currentTab)
            {
                case FinanceOverviewTab.CurrentDay:
                    RenderCurrentDayTab();
                    break;
                case FinanceOverviewTab.NextDay:
                    RenderNextDayTab();
                    break;
                case FinanceOverviewTab.ExpectedIncome:
                    RenderExpectedIncomeTab();
                    break;
                default:
                    RenderPreviousDayTab();
                    break;
            }
        }

        private void RenderPreviousDayTab()
        {
            var snapshot = companyFinanceOverviewManager.GetPreviousDaySnapshot();
            CreateInfoCard($"Dün (Gün {snapshot.Day})\nToplam Gelir: {snapshot.TotalIncome.Amount:N0}\nToplam Gider: {snapshot.TotalExpense.Amount:N0}\nNet: {snapshot.NetAmount.Amount:N0}", 116f);
            RenderLineItems("Gelen Gelirler", snapshot.Incomes, "Dün gelir kaydı yok.");
            RenderLineItems("Yapılan Harcamalar", snapshot.Expenses, "Dün gider kaydı yok.");
        }

        private void RenderCurrentDayTab()
        {
            var daySnapshot = companyFinanceOverviewManager.GetCurrentDaySnapshot();
            CreateInfoCard($"Bugün (Gün {daySnapshot.Day})\nToplam Gelir: {daySnapshot.TotalIncome.Amount:N0}\nToplam Gider: {daySnapshot.TotalExpense.Amount:N0}\nNet: {daySnapshot.NetAmount.Amount:N0}", 116f);
            RenderLineItems("Bugünkü Harcamalar", daySnapshot.Expenses, "Bugün harcama kaydı yok.");
            RenderLineItems("Bugünkü Gelirler", daySnapshot.Incomes, "Bugün gelir kaydı yok.");
        }

        private void RenderNextDayTab()
        {
            var paymentForecast = companyFinanceOverviewManager.GetNextDayPaymentSnapshot();
            var incomeForecast = companyFinanceOverviewManager.GetNextDayIncomeSnapshot();
            CreateInfoCard($"Yarın (Gün {paymentForecast.ReferenceDay})\nToplam Beklenen Ödeme: {paymentForecast.TotalAmount.Amount:N0}\nToplam Beklenen Gelir: {incomeForecast.TotalAmount.Amount:N0}", 108f);
            RenderLineItems("Yarının Ödemeleri", paymentForecast.Items, "Yarın için planlı ödeme görünmüyor.");
            RenderLineItems("Yarının Beklenen Gelirleri", incomeForecast.Items, "Yarın için beklenen gelir görünmüyor.");
        }

        private void RenderExpectedIncomeTab()
        {
            var upcomingIncomeSnapshot = companyFinanceOverviewManager.GetUpcomingIncomeSnapshot();
            CreateInfoCard($"Beklenen Gelirler\nToplam Beklenen Gelir: {upcomingIncomeSnapshot.TotalAmount.Amount:N0}", 78f);
            RenderPlainLineItems(upcomingIncomeSnapshot.Items, "Planlanmış gelecek gelir yok.");
        }

        private void CreateTabRow()
        {
            var row = CreateUiObject("TabRow", contentRoot);
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 10f;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;
            row.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 56f);

            CreateTabButton(row.transform, FinanceOverviewTab.PreviousDay, "Dün");
            CreateTabButton(row.transform, FinanceOverviewTab.CurrentDay, "Bugün");
            CreateTabButton(row.transform, FinanceOverviewTab.NextDay, "Yarın");
            CreateTabButton(row.transform, FinanceOverviewTab.ExpectedIncome, "Beklenen Gelirler");
        }

        private void CreateTabButton(Transform parent, FinanceOverviewTab tab, string label)
        {
            var button = CreateButton(parent, "Tab_" + tab, currentTab == tab ? label + " (Seçili)" : label);
            var layoutElement = button.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 56f;
            button.onClick.AddListener(() =>
            {
                currentTab = tab;
                RefreshPage();
            });
        }

        private void RenderLineItems(string sectionTitle, System.Collections.Generic.IReadOnlyList<FinanceLineItemSnapshot> items, string emptyMessage)
        {
            CreateSectionTitle(sectionTitle);
            RenderPlainLineItems(items, emptyMessage);
        }

        private void RenderPlainLineItems(System.Collections.Generic.IReadOnlyList<FinanceLineItemSnapshot> items, string emptyMessage)
        {
            if (items == null || items.Count == 0)
            {
                CreateInfoCard(emptyMessage, 58f);
                return;
            }

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var detail = string.IsNullOrWhiteSpace(item.Detail) ? string.Empty : "\n" + item.Detail;
                CreateInfoCard($"{item.Title}\nTutar: {item.Amount.Amount:N0}{detail}", 90f);
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
            var button = CreateButton(rootCanvas.transform, "FinanceOverviewOpenButton", "Finans Takibi");
            var buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 1f);
            buttonRect.anchorMax = new Vector2(0f, 1f);
            buttonRect.pivot = new Vector2(0f, 1f);
            buttonRect.anchoredPosition = new Vector2(820f, -80f);
            buttonRect.sizeDelta = new Vector2(220f, 44f);
            button.onClick.AddListener(OpenPanel);
        }

        private void CreatePanel()
        {
            panelRoot = CreateUiObject("FinanceOverviewPanel", rootCanvas.transform);
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

            pageTitleText = CreateText(headerRoot.transform, "Finans Takibi", 28, TextAnchor.MiddleLeft);
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

        private enum FinanceOverviewTab
        {
            PreviousDay = 0,
            CurrentDay = 1,
            NextDay = 2,
            ExpectedIncome = 3
        }
    }
}
