using CompanySimulator.Features.Accounting.Runtime.Components;
using CompanySimulator.Features.Accounting.Runtime.Models;
using CompanySimulator.Features.Employees.Runtime.Definitions;
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
        [SerializeField] private DebugPanelUI debugPanelUI;
        [SerializeField] private SecurityPanelUI securityPanelUI;
        [SerializeField] private ShopPanelUI shopPanelUI;
        [SerializeField] private InventoryPanelUI inventoryPanelUI;
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private Vector2 panelSize = new Vector2(980f, 720f);
        [SerializeField] private float panelVerticalOffset = 72f;

        private static readonly Color ColBg = new Color(0.035f, 0.067f, 0.122f, 0.985f);
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
        private Transform assignedAccountantGridParent;
        private Transform availableAccountantGridParent;

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
            assignedAccountantGridParent = null;
            availableAccountantGridParent = null;

            var snapshot = companyAccountingManager.GetCurrentCycleSnapshot();
            CreateSummaryCards(snapshot);

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
            var button = CreateStyledButton(rootCanvas.transform, "AccountingOpenButton", "Muhasebe", ColSurface, Blend(ColSurface, ColBlue, 0.25f), Darken(ColSurface, 0.16f), ColText, TextAnchor.MiddleCenter);
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
            RuntimePanelUiUtility.ConfigureCenteredPanel(panelRect, panelSize, panelVerticalOffset);
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
            var badgeText = CreateText(badge.transform, "ACC", 16, TextAnchor.MiddleCenter);
            badgeText.color = ColCyan;
            badgeText.fontStyle = FontStyle.Bold;
            RuntimePanelUiUtility.StretchToParent(badgeText.rectTransform, 0f, 0f, 0f, 0f);

            pageTitleText = CreateText(headerRoot.transform, "Muhasebe", 28, TextAnchor.MiddleLeft);
            pageTitleText.color = ColText;
            pageTitleText.fontStyle = FontStyle.Bold;
            pageTitleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            pageTitleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            pageTitleText.rectTransform.offsetMin = new Vector2(86f, -50f);
            pageTitleText.rectTransform.offsetMax = new Vector2(-140f, -14f);

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
        }

        private void CreateAssignedAccountantCard(EmployeeRuntimeData accountant)
        {
            var contribution = Mathf.Max(1, Mathf.CeilToInt(accountant.IncomeMultiplier));
            var canUnassign = companyAccountingManager.CanUnassignAccountant(accountant, out var validationMessage);
            var accent = GetAccountantAccent(accountant);
            var card = CreateSurface(EnsureAssignedAccountantGridHost(), $"Assigned_{accountant.Id}", canUnassign || string.IsNullOrWhiteSpace(validationMessage) ? 196f : 222f, ColPanel);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(400f, canUnassign || string.IsNullOrWhiteSpace(validationMessage) ? 196f : 222f);
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

            var topRow = CreateUiObject("TopRow", content.transform);
            topRow.AddComponent<LayoutElement>().preferredHeight = 26f;
            var topLayout = topRow.AddComponent<HorizontalLayoutGroup>();
            topLayout.spacing = 8f;
            topLayout.childControlWidth = true;
            topLayout.childControlHeight = true;
            topLayout.childForceExpandWidth = false;
            topLayout.childForceExpandHeight = false;
            topLayout.childAlignment = TextAnchor.MiddleLeft;

            var name = CreateText(topRow.transform, accountant.DisplayName, 18, TextAnchor.MiddleLeft);
            name.color = ColText;
            name.fontStyle = FontStyle.Bold;
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            CreateTag(topRow.transform, RuntimePanelUiUtility.GetEmployeeQualityLabel(accountant.QualityTier), new Color(accent.r, accent.g, accent.b, 0.18f), accent, 13);
            CreateTag(topRow.transform, "Atanmış", new Color(ColGreen.r, ColGreen.g, ColGreen.b, 0.18f), ColGreen, 13);

            var statRow = CreateUiObject("StatsRow", content.transform);
            statRow.AddComponent<LayoutElement>().preferredHeight = 42f;
            var statGrid = statRow.AddComponent<GridLayoutGroup>();
            statGrid.cellSize = new Vector2(174f, 42f);
            statGrid.spacing = new Vector2(8f, 0f);
            statGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            statGrid.constraintCount = 2;
            statGrid.childAlignment = TextAnchor.MiddleCenter;

            CreateMiniStat(statRow.transform, "+" + contribution, "Kapasite");
            CreateMiniStat(statRow.transform, accountant.ExpectedDailySalary.Amount.ToString("N0"), "Günlük Maaş");

            if (!canUnassign && !string.IsNullOrWhiteSpace(validationMessage))
            {
                var warning = CreateText(content.transform, validationMessage, 13, TextAnchor.MiddleLeft);
                warning.color = ColMuted;
                warning.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;
            }

            CreateFlexibleSpacer(content.transform);

            var button = CreateStyledButton(content.transform, $"Unassign_{accountant.Id}", canUnassign ? "Şirketten Ayır" : "Aktif işler yüzünden ayrılamaz", canUnassign ? new Color(ColRed.r, ColRed.g, ColRed.b, 0.16f) : ColSurfaceAlt, canUnassign ? new Color(ColRed.r, ColRed.g, ColRed.b, 0.26f) : Blend(ColSurfaceAlt, ColBlue, 0.1f), canUnassign ? new Color(ColRed.r, ColRed.g, ColRed.b, 0.34f) : Darken(ColSurfaceAlt, 0.08f), canUnassign ? ColRed : ColMuted, TextAnchor.MiddleCenter);
            button.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;
            button.interactable = canUnassign;
            button.onClick.AddListener(() =>
            {
                if (companyAccountingManager.TryUnassignAccountant(accountant, out _))
                {
                    RefreshPage();
                }
            });
        }

        private void CreateAvailableAccountantCard(EmployeeRuntimeData accountant)
        {
            var contribution = Mathf.Max(1, Mathf.CeilToInt(accountant.IncomeMultiplier));
            var accent = GetAccountantAccent(accountant);
            var card = CreateSurface(EnsureAvailableAccountantGridHost(), $"Available_{accountant.Id}", 196f, ColPanel);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(400f, 196f);
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

            var topRow = CreateUiObject("TopRow", content.transform);
            topRow.AddComponent<LayoutElement>().preferredHeight = 26f;
            var topLayout = topRow.AddComponent<HorizontalLayoutGroup>();
            topLayout.spacing = 8f;
            topLayout.childControlWidth = true;
            topLayout.childControlHeight = true;
            topLayout.childForceExpandWidth = false;
            topLayout.childForceExpandHeight = false;
            topLayout.childAlignment = TextAnchor.MiddleLeft;

            var name = CreateText(topRow.transform, accountant.DisplayName, 18, TextAnchor.MiddleLeft);
            name.color = ColText;
            name.fontStyle = FontStyle.Bold;
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            CreateTag(topRow.transform, RuntimePanelUiUtility.GetEmployeeQualityLabel(accountant.QualityTier), new Color(accent.r, accent.g, accent.b, 0.18f), accent, 13);
            CreateTag(topRow.transform, "Boşta", new Color(ColBlue.r, ColBlue.g, ColBlue.b, 0.16f), ColBlue, 13);

            var statRow = CreateUiObject("StatsRow", content.transform);
            statRow.AddComponent<LayoutElement>().preferredHeight = 42f;
            var statGrid = statRow.AddComponent<GridLayoutGroup>();
            statGrid.cellSize = new Vector2(174f, 42f);
            statGrid.spacing = new Vector2(8f, 0f);
            statGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            statGrid.constraintCount = 2;
            statGrid.childAlignment = TextAnchor.MiddleCenter;

            CreateMiniStat(statRow.transform, "+" + contribution, "Kapasite");
            CreateMiniStat(statRow.transform, accountant.ExpectedDailySalary.Amount.ToString("N0"), "Günlük Maaş");

            CreateFlexibleSpacer(content.transform);

            var button = CreateStyledButton(content.transform, $"Assign_{accountant.Id}", "Şirkete Ata", ColBlue, Blend(ColBlue, ColCyan, 0.28f), Darken(ColBlue, 0.22f), ColText, TextAnchor.MiddleCenter);
            button.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;
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
            var titleText = CreateText(contentRoot, title, 20, TextAnchor.MiddleLeft);
            titleText.rectTransform.sizeDelta = new Vector2(0f, 34f);
            titleText.color = ColText;
            titleText.fontStyle = FontStyle.Bold;
        }

        private Text CreateInfoCard(string message, float height = 58f)
        {
            var card = CreateSurface(contentRoot, "InfoCard", height, ColSurface);
            var text = CreateText(card.transform, message, 18, TextAnchor.MiddleLeft);
            text.color = ColMuted;
            RuntimePanelUiUtility.StretchToParent(text.rectTransform, 14f, 8f, 14f, 8f);
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
            RuntimePanelUiUtility.StretchToParent(text.rectTransform, 16f, 8f, 16f, 8f);
            return button;
        }

        private void CreateSummaryCards(AccountingCycleSnapshot snapshot)
        {
            const float summaryCardWidth = 286f;
            const float summaryCardHeight = 104f;
            const float summaryGridSpacing = 12f;

            var gridHost = CreateUiObject("SummaryGrid", contentRoot);
            var grid = gridHost.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(summaryCardWidth, summaryCardHeight);
            grid.spacing = new Vector2(summaryGridSpacing, summaryGridSpacing);
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Mathf.Max(1, Mathf.FloorToInt(((panelSize.x - 80f) + summaryGridSpacing) / (summaryCardWidth + summaryGridSpacing)));
            grid.childAlignment = TextAnchor.UpperLeft;

            var fitter = gridHost.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateSummaryCard(gridHost.transform, "İş Kapasitesi", $"{snapshot.ActiveProjectCount} / {snapshot.MaxActiveProjectCount}", "Aktif / Maks", ColBlue);
            CreateSummaryCard(gridHost.transform, "Vergi Zamanı", snapshot.DaysUntilTaxPayment + " gün", "Tahmini: " + snapshot.EstimatedTax.Amount.ToString("N0"), ColGold);
            CreateSummaryCard(gridHost.transform, "Son Vergi", snapshot.LastTaxPayment.Amount.ToString("N0"), "Geçen ödeme", ColRed);
            CreateSummaryCard(gridHost.transform, "Döngü Geliri", snapshot.Income.Amount.ToString("N0"), "Gelir", ColCyan);
            CreateSummaryCard(gridHost.transform, "Döngü Gideri", snapshot.Expenses.Amount.ToString("N0"), "Gider", ColGold);
            CreateSummaryCard(gridHost.transform, "Döngü Kârı", snapshot.Profit.Amount.ToString("N0"), "Net sonuç", snapshot.Profit.Amount >= 0 ? ColGreen : ColRed);
        }

        private void CreateSummaryCard(Transform parent, string titleValue, string valueValue, string footerValue, Color accent)
        {
            var card = CreateSurface(parent, titleValue.Replace(' ', '_') + "Summary", 104f, ColSurface);
            var rect = card.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(286f, 104f);
            var layout = card.GetComponent<LayoutElement>();
            layout.preferredWidth = 286f;
            layout.minWidth = 286f;
            CreateAccentBar(card.transform, accent);

            var content = CreateStretchContainer(card.transform, "Content", 12f, 12f, 12f, 12f);
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 0, 0, 0);
            contentLayout.spacing = 4f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var title = CreateText(content.transform, titleValue, 14, TextAnchor.MiddleLeft);
            title.color = ColMuted;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

            var value = CreateText(content.transform, valueValue, 24, TextAnchor.MiddleLeft);
            value.color = ColText;
            value.fontStyle = FontStyle.Bold;
            value.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

            CreateFlexibleSpacer(content.transform);

            CreateTag(content.transform, footerValue, new Color(accent.r, accent.g, accent.b, 0.18f), accent, 13);
        }

        private Transform EnsureAssignedAccountantGridHost()
        {
            if (assignedAccountantGridParent != null)
            {
                return assignedAccountantGridParent;
            }

            assignedAccountantGridParent = CreateGridHost("AssignedAccountantGrid", 400f, 196f).transform;
            return assignedAccountantGridParent;
        }

        private Transform EnsureAvailableAccountantGridHost()
        {
            if (availableAccountantGridParent != null)
            {
                return availableAccountantGridParent;
            }

            availableAccountantGridParent = CreateGridHost("AvailableAccountantGrid", 400f, 196f).transform;
            return availableAccountantGridParent;
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
            var availableWidth = Mathf.Max(cardWidth, panelSize.x - horizontalPadding);
            return Mathf.Max(1, Mathf.FloorToInt((availableWidth + spacing) / (cardWidth + spacing)));
        }

        private Color GetAccountantAccent(EmployeeRuntimeData accountant)
        {
            if (accountant == null)
            {
                return ColBlue;
            }

            switch (accountant.QualityTier)
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
            RuntimePanelUiUtility.StretchToParent(container.GetComponent<RectTransform>(), left, bottom, right, top);
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

            var tagFitter = tag.AddComponent<ContentSizeFitter>();
            tagFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            tagFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var text = CreateText(tag.transform, value, fontSize, TextAnchor.MiddleCenter);
            text.color = textColor;
            text.fontStyle = FontStyle.Bold;
            var textLayout = text.gameObject.AddComponent<LayoutElement>();
            textLayout.preferredHeight = fontSize >= 14 ? 18f : 16f;
            return tag;
        }

        private GameObject CreateMiniStat(Transform parent, string value, string label)
        {
            var tile = CreateSurface(parent, "MiniStat", 42f, new Color(ColSurfaceAlt.r, ColSurfaceAlt.g, ColSurfaceAlt.b, 0.95f));
            var valueText = CreateText(tile.transform, value, 17, TextAnchor.UpperCenter);
            valueText.color = ColText;
            valueText.fontStyle = FontStyle.Bold;
            RuntimePanelUiUtility.StretchToParent(valueText.rectTransform, 6f, 18f, 6f, 3f);

            var labelText = CreateText(tile.transform, label, 11, TextAnchor.LowerCenter);
            labelText.color = ColMuted;
            RuntimePanelUiUtility.StretchToParent(labelText.rectTransform, 6f, 3f, 6f, 21f);
            return tile;
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
    }
}
