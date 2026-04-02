using CompanySimulator.Features.Inventory.Runtime.Components;
using CompanySimulator.Features.Inventory.Runtime.Models;
using CompanySimulator.Presentation.UI.Runtime.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CompanySimulator.Presentation.UI.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class InventoryPanelUI : MonoBehaviour
    {
        [SerializeField] private InventoryManager inventoryManager;
        [SerializeField] private SectorPanelUI sectorPanelUI;
        [SerializeField] private EmployeePanelUI employeePanelUI;
        [SerializeField] private AccountingPanelUI accountingPanelUI;
        [SerializeField] private BankPanelUI bankPanelUI;
        [SerializeField] private FinanceOverviewPanelUI financeOverviewPanelUI;
        [SerializeField] private RivalCompanyPanelUI rivalCompanyPanelUI;
        [SerializeField] private DebugPanelUI debugPanelUI;
        [SerializeField] private SecurityPanelUI securityPanelUI;
        [SerializeField] private ShopPanelUI shopPanelUI;
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private Vector2 panelSize = new Vector2(1100f, 760f);
        [SerializeField] private float panelVerticalOffset = 72f;

        private static readonly Color ColBg = new Color(0.035f, 0.067f, 0.122f, 0.985f);
        private static readonly Color ColPanel = new Color(0.063f, 0.098f, 0.169f, 1f);
        private static readonly Color ColSurface = new Color(0.082f, 0.125f, 0.204f, 1f);
        private static readonly Color ColSurfaceAlt = new Color(0.047f, 0.078f, 0.141f, 1f);
        private static readonly Color ColText = new Color(0.933f, 0.957f, 1f, 1f);
        private static readonly Color ColMuted = new Color(0.561f, 0.639f, 0.784f, 1f);
        private static readonly Color ColBlue = new Color(0.353f, 0.627f, 1f, 1f);
        private static readonly Color ColCyan = new Color(0.302f, 0.886f, 0.816f, 1f);
        private static readonly Color ColGold = new Color(0.961f, 0.769f, 0.365f, 1f);
        private static readonly Color ColRed = new Color(1f, 0.42f, 0.506f, 1f);

        private Font defaultFont;
        private Sprite roundedSprite;
        private GameObject panelRoot;
        private RectTransform contentRoot;
        private Text pageTitleText;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            inventoryManager ??= FindObjectOfType<InventoryManager>();
            sectorPanelUI ??= FindObjectOfType<SectorPanelUI>();
            employeePanelUI ??= FindObjectOfType<EmployeePanelUI>();
            accountingPanelUI ??= FindObjectOfType<AccountingPanelUI>();
            bankPanelUI ??= FindObjectOfType<BankPanelUI>();
            financeOverviewPanelUI ??= FindObjectOfType<FinanceOverviewPanelUI>();
            rivalCompanyPanelUI ??= FindObjectOfType<RivalCompanyPanelUI>();
            debugPanelUI ??= FindObjectOfType<DebugPanelUI>();
            securityPanelUI ??= FindObjectOfType<SecurityPanelUI>();
            shopPanelUI ??= FindObjectOfType<ShopPanelUI>();
            EnsureCanvas();
            EnsureEventSystem();
            defaultFont = LoadDefaultFont();
            roundedSprite = LoadRoundedSprite();
            BuildUi();
        }

        private void OnEnable()
        {
            if (inventoryManager != null)
            {
                inventoryManager.DataChanged -= RefreshPage;
                inventoryManager.DataChanged += RefreshPage;
            }

            RefreshPage();
        }

        private void OnDisable()
        {
            if (inventoryManager != null)
            {
                inventoryManager.DataChanged -= RefreshPage;
            }
        }

        public void OpenPanel()
        {
            if (sectorPanelUI != null && sectorPanelUI.IsOpen) sectorPanelUI.ClosePanel();
            if (employeePanelUI != null && employeePanelUI.IsOpen) employeePanelUI.ClosePanel();
            if (accountingPanelUI != null && accountingPanelUI.IsOpen) accountingPanelUI.ClosePanel();
            if (bankPanelUI != null && bankPanelUI.IsOpen) bankPanelUI.ClosePanel();
            if (financeOverviewPanelUI != null && financeOverviewPanelUI.IsOpen) financeOverviewPanelUI.ClosePanel();
            if (rivalCompanyPanelUI != null && rivalCompanyPanelUI.IsOpen) rivalCompanyPanelUI.ClosePanel();
            if (debugPanelUI != null && debugPanelUI.IsOpen) debugPanelUI.ClosePanel();
            if (securityPanelUI != null && securityPanelUI.IsOpen) securityPanelUI.ClosePanel();
            if (shopPanelUI != null && shopPanelUI.IsOpen) shopPanelUI.ClosePanel();

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
            if (contentRoot == null)
            {
                return;
            }

            pageTitleText.text = "Envanter / Depo";
            RuntimePanelUiUtility.ClearChildren(contentRoot);

            if (inventoryManager == null)
            {
                CreateInfoCard("InventoryManager sahnede bulunamadı.", 72f);
                return;
            }

            if (!inventoryManager.IsInitialized)
            {
                inventoryManager.Initialize();
            }

            RenderOwnedItems();
            RenderNonInventoryPurchases();
        }

        private void RenderOwnedItems()
        {
            CreateSectionTitle("Depodaki Ürünler");
            var ownedItems = inventoryManager.OwnedItems;
            if (ownedItems.Count == 0)
            {
                CreateInfoCard("Henüz depoya eklenmiş ürün bulunmuyor.", 58f);
                return;
            }

            var gridHost = CreateGridHost("OwnedItemGrid", 400f, 196f);
            for (var i = 0; i < ownedItems.Count; i++)
            {
                CreateOwnedItemCard(gridHost.transform, ownedItems[i]);
            }
        }

        private void RenderNonInventoryPurchases()
        {
            CreateSectionTitle("Direkt Satın Alımlar");
            var purchases = inventoryManager.NonInventoryPurchases;
            if (purchases.Count == 0)
            {
                CreateInfoCard("Envantere gitmeyen satın alım kaydı bulunmuyor.", 58f);
                return;
            }

            var gridHost = CreateGridHost("PurchaseGrid", 400f, 184f);
            for (var i = 0; i < purchases.Count; i++)
            {
                CreateNonInventoryPurchaseCard(gridHost.transform, purchases[i]);
            }
        }

        private void CreateOwnedItemCard(Transform parent, InventoryItemRuntimeData item)
        {
            var accent = ColCyan;
            var cardColor = Blend(ColPanel, accent, 0.08f);
            var card = CreateSurface(parent, "InventoryItem_" + item.Product.Id, 196f, cardColor);
            var rect = card.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400f, 196f);
            var layout = card.GetComponent<LayoutElement>();
            layout.preferredWidth = 400f;
            layout.minWidth = 400f;
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

            var title = CreateText(topRow.transform, item.Product.DisplayName, 18, TextAnchor.MiddleLeft);
            title.color = ColText;
            title.fontStyle = FontStyle.Bold;
            title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            CreateTag(topRow.transform, item.Product.InventoryCategory, new Color(accent.r, accent.g, accent.b, 0.18f), accent, 13);

            var description = CreateText(content.transform, item.Product.Description, 13, TextAnchor.MiddleLeft);
            description.color = ColMuted;
            description.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

            var statsRow = CreateUiObject("StatsRow", content.transform);
            statsRow.AddComponent<LayoutElement>().preferredHeight = 42f;
            var statsGrid = statsRow.AddComponent<GridLayoutGroup>();
            statsGrid.cellSize = new Vector2(116f, 42f);
            statsGrid.spacing = new Vector2(6f, 0f);
            statsGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            statsGrid.constraintCount = 3;
            statsGrid.childAlignment = TextAnchor.MiddleCenter;

            CreateMiniStat(statsRow.transform, item.Quantity.ToString(), "Adet");
            CreateMiniStat(statsRow.transform, item.FirstAcquiredDay.ToString(), "İlk Gün");
            CreateMiniStat(statsRow.transform, item.LastAcquiredDay.ToString(), "Son Gün");

            if (!string.IsNullOrWhiteSpace(item.Product.FutureUsageHint))
            {
                var hint = CreateText(content.transform, item.Product.FutureUsageHint, 12, TextAnchor.MiddleLeft);
                hint.color = new Color(ColText.r, ColText.g, ColText.b, 0.72f);
                hint.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
            }
        }

        private void CreateNonInventoryPurchaseCard(Transform parent, NonInventoryPurchaseRuntimeData purchase)
        {
            var accent = ColGold;
            var cardColor = Blend(ColPanel, accent, 0.08f);
            var card = CreateSurface(parent, "Purchase_" + purchase.Product.Id + "_" + purchase.PurchaseDay, 184f, cardColor);
            var rect = card.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400f, 184f);
            var layout = card.GetComponent<LayoutElement>();
            layout.preferredWidth = 400f;
            layout.minWidth = 400f;
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

            var title = CreateText(content.transform, purchase.Product.DisplayName, 18, TextAnchor.MiddleLeft);
            title.color = ColText;
            title.fontStyle = FontStyle.Bold;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

            var description = CreateText(content.transform, purchase.Product.Description, 13, TextAnchor.MiddleLeft);
            description.color = ColMuted;
            description.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

            var statsRow = CreateUiObject("StatsRow", content.transform);
            statsRow.AddComponent<LayoutElement>().preferredHeight = 42f;
            var statsGrid = statsRow.AddComponent<GridLayoutGroup>();
            statsGrid.cellSize = new Vector2(116f, 42f);
            statsGrid.spacing = new Vector2(6f, 0f);
            statsGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            statsGrid.constraintCount = 3;
            statsGrid.childAlignment = TextAnchor.MiddleCenter;

            CreateMiniStat(statsRow.transform, purchase.Quantity.ToString(), "Adet");
            CreateMiniStat(statsRow.transform, purchase.PurchaseDay.ToString(), "Satın Gün");
            CreateMiniStat(statsRow.transform, purchase.TotalPrice.Amount.ToString("N0"), "Toplam");

            if (!string.IsNullOrWhiteSpace(purchase.Product.FutureUsageHint))
            {
                var hint = CreateText(content.transform, purchase.Product.FutureUsageHint, 12, TextAnchor.MiddleLeft);
                hint.color = new Color(ColText.r, ColText.g, ColText.b, 0.72f);
                hint.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
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
            var button = CreateStyledButton(rootCanvas.transform, "InventoryOpenButton", "Envanter", ColSurface, Blend(ColSurface, ColBlue, 0.25f), Darken(ColSurface, 0.16f), ColText, TextAnchor.MiddleCenter);
            var buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 1f);
            buttonRect.anchorMax = new Vector2(0f, 1f);
            buttonRect.pivot = new Vector2(0f, 1f);
            buttonRect.anchoredPosition = new Vector2(220f, -130f);
            buttonRect.sizeDelta = new Vector2(180f, 44f);
            button.onClick.AddListener(OpenPanel);
        }

        private void CreatePanel()
        {
            panelRoot = CreateUiObject("InventoryPanel", rootCanvas.transform);
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
            var badgeText = CreateText(badge.transform, "INV", 16, TextAnchor.MiddleCenter);
            badgeText.color = ColCyan;
            badgeText.fontStyle = FontStyle.Bold;
            RuntimePanelUiUtility.StretchToParent(badgeText.rectTransform, 0f, 0f, 0f, 0f);

            pageTitleText = CreateText(headerRoot.transform, "Envanter / Depo", 28, TextAnchor.MiddleLeft);
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
            grid.childAlignment = TextAnchor.UpperCenter;
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
            block.GetComponent<RectTransform>().sizeDelta = size;
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

        private GameObject CreateTag(Transform parent, string value, Color bgColor, Color textColor, int fontSize = 12)
        {
            var tag = CreateUiObject("Tag", parent);
            ApplyRoundedImage(tag, bgColor);
            tag.AddComponent<LayoutElement>().preferredHeight = fontSize >= 14 ? 30f : 26f;
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
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = fontSize >= 14 ? 18f : 16f;
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

        private Text CreateInfoCard(string message, float height = 58f)
        {
            var card = CreateSurface(contentRoot, "InfoCard", height, ColSurface);
            var text = CreateText(card.transform, message, 18, TextAnchor.MiddleLeft);
            text.color = ColMuted;
            RuntimePanelUiUtility.StretchToParent(text.rectTransform, 14f, 8f, 14f, 8f);
            return text;
        }

        private void CreateSectionTitle(string title)
        {
            var titleText = CreateText(contentRoot, title, 20, TextAnchor.MiddleLeft);
            titleText.rectTransform.sizeDelta = new Vector2(0f, 34f);
            titleText.color = ColText;
            titleText.fontStyle = FontStyle.Bold;
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

        private Text CreateText(Transform parent, string value, int fontSize, TextAnchor anchor)
        {
            return RuntimePanelUiUtility.CreateText(parent, defaultFont, value, fontSize, anchor);
        }

        private GameObject CreateUiObject(string objectName, Transform parent)
        {
            return RuntimePanelUiUtility.CreateUiObject(objectName, parent);
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
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
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
