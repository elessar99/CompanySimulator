using CompanySimulator.Features.Employees.Runtime.Components;
using CompanySimulator.Features.Employees.Runtime.Definitions;
using CompanySimulator.Features.Employees.Runtime.Models;
using CompanySimulator.Features.Npcs.Runtime.Interview;
using CompanySimulator.Presentation.UI.Runtime.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CompanySimulator.Presentation.UI.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class EmployeeDevelopmentPanelUI : MonoBehaviour
    {
        [SerializeField] private EmployeeManager employeeManager;
        [SerializeField] private InterviewSessionManager interviewSessionManager;
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private Sprite appIcon;

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

        private const float RequestCardWidth = 380f;
        private const float RequestCardHeight = 252f;
        private const float GridSpacing = 36f;
        private const float FallbackPanelWidth = 980f;

        private Font defaultFont;
        private Sprite roundedSprite;
        private GameObject panelRoot;
        private RectTransform contentRoot;
        private Transform requestGridParent;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            employeeManager ??= FindObjectOfType<EmployeeManager>();
            interviewSessionManager ??= FindObjectOfType<InterviewSessionManager>();
            rootCanvas ??= FindObjectOfType<Canvas>();
            EnsureEventSystem();
            defaultFont = LoadDefaultFont();
            roundedSprite = LoadRoundedSprite();
            BuildUi();
        }

        private void OnEnable()
        {
            if (employeeManager != null)
            {
                employeeManager.DataChanged -= Refresh;
                employeeManager.DataChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (employeeManager != null)
            {
                employeeManager.DataChanged -= Refresh;
            }
        }

        public void OpenPanel()
        {
            RuntimePanelUiUtility.SetComputerPanelActive(rootCanvas, true);
            panelRoot.SetActive(true);
            RuntimePanelUiUtility.BringToFront(panelRoot);
            Refresh();
        }

        public void ClosePanel()
        {
            panelRoot.SetActive(false);
        }

        private void BuildUi()
        {
            if (rootCanvas == null)
            {
                return;
            }

            RuntimePanelUiUtility.EnsureResponsiveCanvasScaler(rootCanvas);
            var button = RuntimePanelUiUtility.CreateDesktopAppButton(
                RuntimePanelUiUtility.GetOrCreateComputerDesktopIconRoot(rootCanvas),
                defaultFont,
                "EmployeeDevelopmentOpenButton",
                "Talep Paneli",
                appIcon,
                "TLP",
                ColGold);
            button.onClick.AddListener(OpenPanel);

            panelRoot = CreateUiObject("EmployeeDevelopmentPanel", RuntimePanelUiUtility.GetOrCreateComputerWindowRoot(rootCanvas));
            RuntimePanelUiUtility.ConfigureFillComputerPanelChild(panelRoot.GetComponent<RectTransform>(), rootCanvas);
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

            var badge = CreateRoundedBlock(headerRoot.transform, "HeaderBadge", new Vector2(48f, 48f), new Color(ColGold.r, ColGold.g, ColGold.b, 0.18f));
            var badgeRect = badge.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0f, 0.5f);
            badgeRect.anchorMax = new Vector2(0f, 0.5f);
            badgeRect.pivot = new Vector2(0f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(18f, 0f);
            var badgeText = CreateText(badge.transform, "TLP", 16, TextAnchor.MiddleCenter);
            badgeText.color = ColGold;
            badgeText.fontStyle = FontStyle.Bold;
            StretchToParent(badgeText.rectTransform, 0f, 0f, 0f, 0f);

            var title = CreateText(headerRoot.transform, "Talep Paneli", 28, TextAnchor.MiddleLeft);
            title.color = ColText;
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.offsetMin = new Vector2(86f, -50f);
            title.rectTransform.offsetMax = new Vector2(-82f, -14f);

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
            contentRoot.sizeDelta = Vector2.zero;

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = GridSpacing;
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
            panelRoot.SetActive(false);
        }

        private void Refresh()
        {
            if (panelRoot == null || contentRoot == null || employeeManager == null)
            {
                return;
            }

            if (!employeeManager.IsInitialized)
            {
                employeeManager.Initialize();
            }

            interviewSessionManager ??= FindObjectOfType<InterviewSessionManager>();
            requestGridParent = null;
            RuntimePanelUiUtility.ClearChildren(contentRoot);
            var requests = employeeManager.GetEmployeesWithQualityUpgradeRequests();
            if (requests.Count == 0)
            {
                CreateInfoCard("Şu anda maaş düzenlemesi isteyen çalışan yok.", 72f);
                return;
            }

            for (var i = 0; i < requests.Count; i++)
            {
                CreateRequestCard(requests[i]);
            }
        }

        private void CreateRequestCard(EmployeeRuntimeData employee)
        {
            var accent = GetEmployeeAccent(employee);
            var cardColor = Blend(ColPanel, accent, 0.12f);
            var card = CreateSurface(EnsureRequestGridHost(), $"Request_{employee.Id}", RequestCardHeight, cardColor);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(RequestCardWidth, RequestCardHeight);
            var cardLayout = card.GetComponent<LayoutElement>();
            cardLayout.preferredWidth = RequestCardWidth;
            cardLayout.minWidth = RequestCardWidth;
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
            topRow.AddComponent<LayoutElement>().preferredHeight = 30f;
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

            var statusAccent = employee.IsQualityUpgradeNegotiationActive ? ColBlue : ColGold;
            CreateTag(topRow.transform, employee.IsQualityUpgradeNegotiationActive ? "Görüşme" : GetRemainingDaysLabel(employee), new Color(statusAccent.r, statusAccent.g, statusAccent.b, 0.18f), statusAccent, 13);

            var sectorName = employeeManager != null ? employeeManager.GetEmployeeAssignedSectorName(employee) : "Boşta";
            var sector = CreateText(content.transform, "Sektör: " + sectorName, 14, TextAnchor.MiddleLeft);
            sector.color = ColMuted;
            sector.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

            var statRow = CreateUiObject("StatsRow", content.transform);
            statRow.AddComponent<LayoutElement>().preferredHeight = 92f;
            var statGrid = statRow.AddComponent<GridLayoutGroup>();
            statGrid.cellSize = new Vector2(174f, 42f);
            statGrid.spacing = new Vector2(8f, 8f);
            statGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            statGrid.constraintCount = 2;
            statGrid.childAlignment = TextAnchor.MiddleCenter;

            CreateMiniStat(statRow.transform, employee.EffectiveDailySalary.Amount.ToString("N0"), "Mevcut Maaş");
            CreateMiniStat(statRow.transform, employeeManager.CalculateSeverancePay(employee).Amount.ToString("N0"), "Tazminat");
            CreateMiniStat(statRow.transform, GetQualityTransitionLabel(employee), "Kalite");
            CreateMiniStat(statRow.transform, GetRemainingDaysLabel(employee), "Görüşme Süresi");

            CreateFlexibleSpacer(content.transform);

            var canStartInterview = !employee.IsQualityUpgradeNegotiationActive
                && interviewSessionManager != null
                && !interviewSessionManager.HasActiveSession;
            var buttonLabel = employee.IsQualityUpgradeNegotiationActive
                ? "Görüşme Sürüyor"
                : interviewSessionManager != null && interviewSessionManager.HasActiveSession ? "Görüşme Meşgul" : "Görüşme Başlat";
            var buttonNormal = canStartInterview ? ColBlue : new Color(ColSurfaceAlt.r, ColSurfaceAlt.g, ColSurfaceAlt.b, 0.95f);
            var startButton = CreateStyledButton(content.transform, $"StartInterview_{employee.Id}", buttonLabel, buttonNormal, Blend(buttonNormal, ColCyan, 0.18f), Darken(buttonNormal, 0.18f), canStartInterview ? ColText : ColMuted, TextAnchor.MiddleCenter);
            startButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;
            startButton.interactable = canStartInterview;
            startButton.onClick.AddListener(() =>
            {
                interviewSessionManager ??= FindObjectOfType<InterviewSessionManager>();
                if (interviewSessionManager != null && interviewSessionManager.TryStartQualityUpgradeInterview(employee))
                {
                    ClosePanel();
                    RuntimePanelUiUtility.SetComputerPanelActive(rootCanvas, false);
                    return;
                }

                Refresh();
            });
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
            var tile = CreateSurface(parent, "MiniStat", 42f, new Color(ColSurfaceAlt.r, ColSurfaceAlt.g, ColSurfaceAlt.b, 0.95f));
            var valueText = CreateText(tile.transform, value, 15, TextAnchor.UpperCenter);
            valueText.color = ColText;
            valueText.fontStyle = FontStyle.Bold;
            valueText.horizontalOverflow = HorizontalWrapMode.Overflow;
            valueText.verticalOverflow = VerticalWrapMode.Truncate;
            StretchToParent(valueText.rectTransform, 6f, 17f, 6f, 3f);

            var labelText = CreateText(tile.transform, label, 10, TextAnchor.LowerCenter);
            labelText.color = ColMuted;
            labelText.verticalOverflow = VerticalWrapMode.Truncate;
            StretchToParent(labelText.rectTransform, 6f, 3f, 6f, 20f);
            return tile;
        }

        private Transform EnsureRequestGridHost()
        {
            if (requestGridParent != null)
            {
                return requestGridParent;
            }

            var host = CreateUiObject("RequestGrid", contentRoot);
            var grid = host.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(RequestCardWidth, RequestCardHeight);
            grid.spacing = new Vector2(GridSpacing, GridSpacing);
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = CalculateGridColumnCount(RequestCardWidth, GridSpacing);
            grid.childAlignment = TextAnchor.UpperCenter;

            var fitter = host.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            requestGridParent = host.transform;
            return requestGridParent;
        }

        private int CalculateGridColumnCount(float cardWidth, float spacing)
        {
            const float horizontalPadding = 80f;
            var referenceWidth = contentRoot != null && contentRoot.rect.width > 0f
                ? contentRoot.rect.width
                : panelRoot != null ? panelRoot.GetComponent<RectTransform>().rect.width : FallbackPanelWidth;
            var availableWidth = Mathf.Max(cardWidth, referenceWidth - horizontalPadding);
            return Mathf.Max(1, Mathf.FloorToInt((availableWidth + spacing) / (cardWidth + spacing)));
        }

        private static string GetQualityTransitionLabel(EmployeeRuntimeData employee)
        {
            if (employee == null)
            {
                return "-";
            }

            var targetTier = employee.IsQualityUpgradeNegotiationActive ? employee.QualityTier : employee.PendingQualityUpgradeTier;
            return RuntimePanelUiUtility.GetEmployeeQualityLabel(employee.QualityUpgradeSourceTier) + " -> " + RuntimePanelUiUtility.GetEmployeeQualityLabel(targetTier);
        }

        private static string GetRemainingDaysLabel(EmployeeRuntimeData employee)
        {
            if (employee == null)
            {
                return "-";
            }

            return employee.IsQualityUpgradeNegotiationActive
                ? "Başladı"
                : Mathf.Max(0, employee.QualityUpgradeRequestRemainingDays) + "g";
        }

        private Color GetEmployeeAccent(EmployeeRuntimeData employee)
        {
            if (employee == null)
            {
                return ColBlue;
            }

            var targetTier = employee.IsQualityUpgradeNegotiationActive ? employee.QualityTier : employee.PendingQualityUpgradeTier;
            switch (targetTier)
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

        private static Font LoadDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }
}
