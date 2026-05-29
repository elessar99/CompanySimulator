using CompanySimulator.Presentation.UI.Runtime.Common;
using UnityEngine;
using UnityEngine.UI;

namespace CompanySimulator.Presentation.UI.Runtime.Components
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ComputerPanelUI : MonoBehaviour
    {
        [SerializeField] private Canvas rootCanvas;
        [SerializeField, Min(0f)] private float topMargin = 120f;
        [SerializeField, Min(0f)] private float bottomMargin = 140f;
        [SerializeField, Min(0f)] private float panelPaddingLeft = 18f;
        [SerializeField, Min(0f)] private float panelPaddingRight = 18f;
        [SerializeField, Min(0f)] private float panelPaddingTop = 18f;
        [SerializeField, Min(0f)] private float panelPaddingBottom = 18f;
        [SerializeField, Min(4f)] private float borderThickness = 18f;
        [SerializeField] private bool startVisible;
        [SerializeField] private Vector2 desktopIconCellSize = new Vector2(96f, 112f);
        [SerializeField] private Vector2 desktopIconSpacing = new Vector2(18f, 16f);
        [SerializeField, Min(0f)] private float desktopIconPaddingLeft = 18f;
        [SerializeField, Min(0f)] private float desktopIconPaddingRight = 18f;
        [SerializeField, Min(0f)] private float desktopIconPaddingTop = 18f;
        [SerializeField, Min(0f)] private float desktopIconPaddingBottom = 18f;

        private RectTransform rectTransform;
        private RectTransform desktopIconRoot;
        private RectTransform windowRoot;
        private bool isApplyingLayout;

        public Canvas RootCanvas => rootCanvas;
        public RectTransform PanelRoot => rectTransform != null ? rectTransform : (RectTransform)transform;
        public RectTransform ContentRoot => windowRoot != null ? windowRoot : PanelRoot;
        public RectTransform DesktopIconRoot => desktopIconRoot != null ? desktopIconRoot : PanelRoot;
        public float TopMargin => topMargin;
        public float BottomMargin => bottomMargin;
        public float PanelPaddingLeft => panelPaddingLeft;
        public float PanelPaddingRight => panelPaddingRight;
        public float PanelPaddingTop => panelPaddingTop;
        public float PanelPaddingBottom => panelPaddingBottom;
        public float BorderThickness => borderThickness;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            rootCanvas ??= GetComponentInParent<Canvas>();
            if (rootCanvas != null)
            {
                RuntimePanelUiUtility.EnsureResponsiveCanvasScaler(rootCanvas);
            }

            EnsureChildRoots();
            ApplyLayout();
            // gameObject.SetActive(startVisible);
        }

        private void Start()
        {
            ApplyLayout();
            gameObject.SetActive(startVisible);
        }

        private void OnValidate()
        {
            rectTransform = transform as RectTransform;
            if (rectTransform != null && rootCanvas != null)
            {
                ApplyLayout();
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyLayout();
        }

        private void LateUpdate()
        {
            RefreshRootOrdering();
        }

        public void SetVisible(bool isVisible)
        {
            gameObject.SetActive(isVisible);
            if (isVisible)
            {
                ApplyLayout();
            }
        }

        public bool ToggleVisible()
        {
            var nextState = !gameObject.activeSelf;
            gameObject.SetActive(nextState);
            if (nextState)
            {
                ApplyLayout();
            }

            return nextState;
        }

        public bool HasVisibleWindowOpen()
        {
            return HasVisibleWindow();
        }

        public bool TryCloseTopWindow()
        {
            if (windowRoot == null)
            {
                return false;
            }

            for (var i = windowRoot.childCount - 1; i >= 0; i--)
            {
                var child = windowRoot.GetChild(i);
                if (child != null && child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(false);
                    return true;
                }
            }

            return false;
        }

        public void ApplyLayout()
        {
            if (isApplyingLayout)
            {
                return;
            }

            if (rectTransform == null)
            {
                rectTransform = transform as RectTransform;
            }

            rootCanvas ??= GetComponentInParent<Canvas>();
            if (rectTransform == null || rootCanvas == null)
            {
                return;
            }

            var pixelRect = rootCanvas.pixelRect;
            if (pixelRect.width <= 0f || pixelRect.height <= 0f)
            {
                return;
            }

            isApplyingLayout = true;
            try
            {
                var widthScale = pixelRect.width / 1920f;
                var heightScale = pixelRect.height / 1080f;
                var topGap = Mathf.Max(0f, topMargin * heightScale);
                var bottomGap = Mathf.Max(0f, bottomMargin * heightScale);
                var sideGap = Mathf.Max(24f, 48f * widthScale);
                var availableWidth = Mathf.Max(320f, pixelRect.width - (sideGap * 2f));
                var availableHeight = Mathf.Max(220f, pixelRect.height - topGap - bottomGap);

                const float targetAspect = 17f / 9f;
                var width = availableHeight * targetAspect;
                var height = availableHeight;

                if (width > availableWidth)
                {
                    width = availableWidth;
                    height = width / targetAspect;
                }

                rectTransform.anchorMin = new Vector2(0.5f, 1f);
                rectTransform.anchorMax = new Vector2(0.5f, 1f);
                rectTransform.pivot = new Vector2(0.5f, 1f);
                rectTransform.anchoredPosition = new Vector2(0f, -topGap);
                rectTransform.sizeDelta = new Vector2(width, height);

                EnsureChildRoots();
                ApplyDesktopIconLayout(widthScale, heightScale);
                ApplyWindowRootLayout(widthScale, heightScale);
            }
            finally
            {
                isApplyingLayout = false;
            }
        }

        public void ApplyChildPanelLayout(RectTransform childRect)
        {
            if (childRect == null)
            {
                return;
            }

            RuntimePanelUiUtility.ConfigureFillParentPanel(childRect);
        }

        private void EnsureChildRoots()
        {
            if (desktopIconRoot == null)
            {
                var existingDesktop = transform.Find("DesktopIconRoot") as RectTransform;
                desktopIconRoot = existingDesktop != null
                    ? existingDesktop
                    : RuntimePanelUiUtility.CreateUiObject("DesktopIconRoot", transform).GetComponent<RectTransform>();
            }

            if (windowRoot == null)
            {
                var existingWindow = transform.Find("WindowRoot") as RectTransform;
                windowRoot = existingWindow != null
                    ? existingWindow
                    : RuntimePanelUiUtility.CreateUiObject("WindowRoot", transform).GetComponent<RectTransform>();
            }

            if (desktopIconRoot.GetComponent<GridLayoutGroup>() == null)
            {
                desktopIconRoot.gameObject.AddComponent<GridLayoutGroup>();
            }

            if (desktopIconRoot.GetComponent<ContentSizeFitter>() == null)
            {
                var fitter = desktopIconRoot.gameObject.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            }

            RefreshRootOrdering();
        }

        private void ApplyDesktopIconLayout(float widthScale, float heightScale)
        {
            if (desktopIconRoot == null)
            {
                return;
            }

            desktopIconRoot.anchorMin = Vector2.zero;
            desktopIconRoot.anchorMax = Vector2.one;
            desktopIconRoot.pivot = new Vector2(0f, 1f);
            desktopIconRoot.anchoredPosition = Vector2.zero;
            desktopIconRoot.offsetMin = new Vector2(desktopIconPaddingLeft * widthScale, desktopIconPaddingBottom * heightScale);
            desktopIconRoot.offsetMax = new Vector2(-(desktopIconPaddingRight * widthScale), -(desktopIconPaddingTop * heightScale));

            var grid = desktopIconRoot.GetComponent<GridLayoutGroup>();
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Vertical;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            grid.cellSize = new Vector2(desktopIconCellSize.x * widthScale, desktopIconCellSize.y * heightScale);
            grid.spacing = new Vector2(desktopIconSpacing.x * widthScale, desktopIconSpacing.y * heightScale);
            grid.padding = new RectOffset(0, 0, 0, 0);

            var usableHeight = Mathf.Max(grid.cellSize.y, desktopIconRoot.rect.height);
            grid.constraintCount = Mathf.Max(1, Mathf.FloorToInt((usableHeight + grid.spacing.y) / (grid.cellSize.y + grid.spacing.y)));
            LayoutRebuilder.MarkLayoutForRebuild(desktopIconRoot);
        }

        private void ApplyWindowRootLayout(float widthScale, float heightScale)
        {
            if (windowRoot == null)
            {
                return;
            }

            windowRoot.anchorMin = Vector2.zero;
            windowRoot.anchorMax = Vector2.one;
            windowRoot.pivot = new Vector2(0.5f, 0.5f);
            windowRoot.anchoredPosition = Vector2.zero;
            windowRoot.offsetMin = new Vector2(panelPaddingLeft * widthScale, panelPaddingBottom * heightScale);
            windowRoot.offsetMax = new Vector2(-(panelPaddingRight * widthScale), -(panelPaddingTop * heightScale));
        }

        private void RefreshRootOrdering()
        {
            if (desktopIconRoot == null || windowRoot == null)
            {
                return;
            }

            if (HasVisibleWindow())
            {
                desktopIconRoot.SetAsFirstSibling();
                windowRoot.SetAsLastSibling();
                return;
            }

            windowRoot.SetAsFirstSibling();
            desktopIconRoot.SetAsLastSibling();
        }

        private bool HasVisibleWindow()
        {
            for (var i = 0; i < windowRoot.childCount; i++)
            {
                var child = windowRoot.GetChild(i);
                if (child != null && child.gameObject.activeSelf)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
