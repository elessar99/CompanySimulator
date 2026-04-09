using CompanySimulator.Presentation.UI.Runtime.Common;
using UnityEngine;

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

        private RectTransform rectTransform;

        public Canvas RootCanvas => rootCanvas;
        public RectTransform PanelRoot => rectTransform != null ? rectTransform : (RectTransform)transform;
        public RectTransform ContentRoot => PanelRoot;
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

        public void SetVisible(bool isVisible)
        {
            gameObject.SetActive(isVisible);
        }

        public bool ToggleVisible()
        {
            var nextState = !gameObject.activeSelf;
            gameObject.SetActive(nextState);
            return nextState;
        }

        public void ApplyLayout()
        {
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
        }

        public void ApplyChildPanelLayout(RectTransform childRect)
        {
            if (childRect == null)
            {
                return;
            }

            rootCanvas ??= GetComponentInParent<Canvas>();
            var pixelRect = rootCanvas != null ? rootCanvas.pixelRect : new Rect(0f, 0f, 1920f, 1080f);
            var widthScale = pixelRect.width > 0f ? pixelRect.width / 1920f : 1f;
            var heightScale = pixelRect.height > 0f ? pixelRect.height / 1080f : 1f;

            childRect.anchorMin = Vector2.zero;
            childRect.anchorMax = Vector2.one;
            childRect.pivot = new Vector2(0.5f, 0.5f);
            childRect.anchoredPosition = Vector2.zero;
            childRect.offsetMin = new Vector2(panelPaddingLeft * widthScale, panelPaddingBottom * heightScale);
            childRect.offsetMax = new Vector2(-(panelPaddingRight * widthScale), -(panelPaddingTop * heightScale));
        }
    }
}
