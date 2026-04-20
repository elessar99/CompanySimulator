using CompanySimulator.Features.Furniture.Runtime.Components;
using CompanySimulator.Features.Inventory.Runtime.Components;
using CompanySimulator.Features.Inventory.Runtime.Models;
using CompanySimulator.Presentation.UI.Runtime.Common;
using UnityEngine;
using UnityEngine.UI;

namespace CompanySimulator.Presentation.UI.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class BuildModePanelUI : MonoBehaviour
    {
        [SerializeField] private InventoryManager inventoryManager;
        [SerializeField] private FurniturePlacementManager furniturePlacementManager;
        [SerializeField] private Canvas rootCanvas;

        private static readonly Color ColPanel = new Color(0.035f, 0.067f, 0.122f, 0.94f);
        private static readonly Color ColSurface = new Color(0.063f, 0.098f, 0.169f, 0.98f);
        private static readonly Color ColSurfaceAlt = new Color(0.082f, 0.125f, 0.204f, 0.98f);
        private static readonly Color ColText = new Color(0.933f, 0.957f, 1f, 1f);
        private static readonly Color ColMuted = new Color(0.561f, 0.639f, 0.784f, 1f);
        private static readonly Color ColBlue = new Color(0.353f, 0.627f, 1f, 1f);
        private static readonly Color ColCyan = new Color(0.302f, 0.886f, 0.816f, 1f);

        private Font defaultFont;
        private GameObject panelRoot;
        private RectTransform contentRoot;
        private Text statusText;

        private void Awake()
        {
            inventoryManager ??= FindObjectOfType<InventoryManager>();
            furniturePlacementManager ??= FindObjectOfType<FurniturePlacementManager>();
            rootCanvas ??= FindObjectOfType<Canvas>();
            defaultFont = LoadDefaultFont();
            BuildUi();
        }

        private void OnEnable()
        {
            if (inventoryManager != null)
            {
                inventoryManager.DataChanged -= Refresh;
                inventoryManager.DataChanged += Refresh;
            }

            if (furniturePlacementManager != null)
            {
                furniturePlacementManager.PlacementModeChanged -= Refresh;
                furniturePlacementManager.PlacementModeChanged += Refresh;
                furniturePlacementManager.PlacementChanged -= Refresh;
                furniturePlacementManager.PlacementChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (inventoryManager != null)
            {
                inventoryManager.DataChanged -= Refresh;
            }

            if (furniturePlacementManager != null)
            {
                furniturePlacementManager.PlacementModeChanged -= Refresh;
                furniturePlacementManager.PlacementChanged -= Refresh;
            }
        }

        private void BuildUi()
        {
            rootCanvas ??= FindObjectOfType<Canvas>();
            if (rootCanvas == null)
            {
                return;
            }

            var hudRoot = RuntimePanelUiUtility.GetOrCreateHudRoot(rootCanvas);
            if (hudRoot == null)
            {
                return;
            }

            panelRoot = RuntimePanelUiUtility.CreateUiObject("BuildModePanel", hudRoot.gameObject.transform);
            var panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 18f);
            panelRect.sizeDelta = new Vector2(1380f, 208f);
            var panelImage = panelRoot.AddComponent<Image>();
            panelImage.color = ColPanel;

            var header = RuntimePanelUiUtility.CreateUiObject("Header", panelRoot.transform);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0f, 38f);
            header.AddComponent<Image>().color = ColSurface;

            var title = RuntimePanelUiUtility.CreateText(header.transform, defaultFont, "Build Modu", 20, TextAnchor.MiddleLeft);
            title.color = ColText;
            title.fontStyle = FontStyle.Bold;
            RuntimePanelUiUtility.StretchToParent(title.rectTransform, 16f, 6f, 760f, 6f);

            statusText = RuntimePanelUiUtility.CreateText(header.transform, defaultFont, string.Empty, 15, TextAnchor.MiddleRight);
            statusText.color = ColMuted;
            RuntimePanelUiUtility.StretchToParent(statusText.rectTransform, 520f, 6f, 16f, 6f);

            var scrollRoot = RuntimePanelUiUtility.CreateUiObject("ScrollRoot", panelRoot.transform);
            var scrollRectTransform = scrollRoot.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0f, 0f);
            scrollRectTransform.anchorMax = new Vector2(1f, 1f);
            scrollRectTransform.offsetMin = new Vector2(12f, 12f);
            scrollRectTransform.offsetMax = new Vector2(-12f, -48f);
            scrollRoot.AddComponent<Image>().color = ColSurfaceAlt;

            var scrollRect = scrollRoot.AddComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;

            var viewport = RuntimePanelUiUtility.CreateUiObject("Viewport", scrollRoot.transform);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(6f, 6f);
            viewportRect.offsetMax = new Vector2(-6f, -6f);
            viewport.AddComponent<RectMask2D>();
            viewport.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);

            var content = RuntimePanelUiUtility.CreateUiObject("Content", viewport.transform);
            contentRoot = content.GetComponent<RectTransform>();
            contentRoot.anchorMin = new Vector2(0f, 0.5f);
            contentRoot.anchorMax = new Vector2(0f, 0.5f);
            contentRoot.pivot = new Vector2(0f, 0.5f);
            contentRoot.anchoredPosition = Vector2.zero;
            contentRoot.sizeDelta = Vector2.zero;

            var layout = content.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleLeft;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRoot;
            panelRoot.SetActive(false);
        }

        private void Refresh()
        {
            if (panelRoot == null || contentRoot == null || furniturePlacementManager == null)
            {
                return;
            }

            panelRoot.SetActive(furniturePlacementManager.IsBuildModeActive);
            if (!panelRoot.activeSelf)
            {
                return;
            }

            RuntimePanelUiUtility.ClearChildren(contentRoot);
            RefreshStatus();

            inventoryManager ??= FindObjectOfType<InventoryManager>();
            if (inventoryManager == null)
            {
                return;
            }

            if (!inventoryManager.IsInitialized)
            {
                inventoryManager.Initialize();
            }

            var furnitureItems = inventoryManager.GetOwnedFurnitureItems();
            for (var i = 0; i < furnitureItems.Count; i++)
            {
                CreateFurnitureCard(furnitureItems[i]);
            }
        }

        private void RefreshStatus()
        {
            if (statusText == null || furniturePlacementManager == null)
            {
                return;
            }

            if (furniturePlacementManager.HasPendingPlacement)
            {
                statusText.text = "Baktığın yere yerleştirmek için E, döndürmek için R, seçimi iptal için Q, build modunu kapatmak için B.";
                return;
            }

            statusText.text = "B build modunu kapatır. Alttaki bir mobilyada Yerleştir'e basıp sonra dünyada E ile koy.";
        }

        private void CreateFurnitureCard(InventoryItemRuntimeData item)
        {
            if (item == null || item.Product == null)
            {
                return;
            }

            var card = RuntimePanelUiUtility.CreateUiObject("BuildItem_" + item.Product.Id, contentRoot);
            var rect = card.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(252f, 134f);
            var layout = card.AddComponent<LayoutElement>();
            layout.preferredWidth = 252f;
            layout.preferredHeight = 134f;
            card.AddComponent<Image>().color = item.Product == furniturePlacementManager.PendingProduct ? new Color(ColBlue.r, ColBlue.g, ColBlue.b, 0.34f) : ColSurface;

            var title = RuntimePanelUiUtility.CreateText(card.transform, defaultFont, item.Product.DisplayName, 17, TextAnchor.UpperLeft);
            title.color = ColText;
            title.fontStyle = FontStyle.Bold;
            RuntimePanelUiUtility.StretchToParent(title.rectTransform, 12f, 82f, 12f, 12f);

            var subtitle = RuntimePanelUiUtility.CreateText(card.transform, defaultFont, $"Tier {item.FurnitureTier}  •  Adet {item.Quantity}", 13, TextAnchor.UpperLeft);
            subtitle.color = ColMuted;
            RuntimePanelUiUtility.StretchToParent(subtitle.rectTransform, 12f, 60f, 12f, 34f);

            var hint = RuntimePanelUiUtility.CreateText(card.transform, defaultFont, item.Product.FurnitureDefinition != null ? item.Product.FurnitureDefinition.Category : "Mobilya", 12, TextAnchor.UpperLeft);
            hint.color = ColCyan;
            RuntimePanelUiUtility.StretchToParent(hint.rectTransform, 12f, 44f, 12f, 52f);

            var placeButton = RuntimePanelUiUtility.CreateButton(card.transform, defaultFont, "PlaceButton", item.Product == furniturePlacementManager.PendingProduct ? "Seçildi" : "Yerleştir");
            var buttonRect = placeButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 0f);
            buttonRect.anchorMax = new Vector2(1f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.offsetMin = new Vector2(12f, 10f);
            buttonRect.offsetMax = new Vector2(-12f, 44f);
            placeButton.onClick.AddListener(() =>
            {
                if (furniturePlacementManager != null)
                {
                    furniturePlacementManager.BeginPlacement(item.Product, out _);
                }
            });
        }

        private Font LoadDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
