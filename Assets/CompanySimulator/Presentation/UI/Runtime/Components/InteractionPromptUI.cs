using CompanySimulator.Presentation.UI.Runtime.Common;
using UnityEngine;
using UnityEngine.UI;

namespace CompanySimulator.Presentation.UI.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private Vector2 panelSize = new Vector2(820f, 64f);
        [SerializeField] private Vector2 anchoredPosition = new Vector2(0f, -270f);

        private RectTransform panelRoot;
        private Text promptText;
        private Font defaultFont;

        private void Awake()
        {
            defaultFont = LoadDefaultFont();
            EnsureLayout();
            SetPrompt(string.Empty);
        }

        public void SetRootCanvas(Canvas canvas)
        {
            if (rootCanvas == canvas)
            {
                return;
            }

            rootCanvas = canvas;
            panelRoot = null;
            promptText = null;
            EnsureLayout();
        }

        public void SetPrompt(string prompt)
        {
            EnsureLayout();
            if (panelRoot == null || promptText == null)
            {
                return;
            }

            var shouldShow = !string.IsNullOrWhiteSpace(prompt);
            panelRoot.gameObject.SetActive(shouldShow);
            promptText.text = shouldShow ? prompt : string.Empty;
        }

        private void EnsureLayout()
        {
            rootCanvas ??= GetComponentInParent<Canvas>();
            rootCanvas ??= FindObjectOfType<Canvas>();
            if (rootCanvas == null)
            {
                return;
            }

            RuntimePanelUiUtility.EnsureResponsiveCanvasScaler(rootCanvas);
            var hudRoot = RuntimePanelUiUtility.GetOrCreateHudRoot(rootCanvas);
            if (hudRoot == null)
            {
                return;
            }

            if (panelRoot == null)
            {
                var existing = hudRoot.Find("InteractionPromptPanel") as RectTransform;
                panelRoot = existing != null
                    ? existing
                    : RuntimePanelUiUtility.CreateUiObject("InteractionPromptPanel", hudRoot).GetComponent<RectTransform>();
            }

            panelRoot.SetParent(hudRoot, false);
            panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
            panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
            panelRoot.pivot = new Vector2(0.5f, 0.5f);
            panelRoot.anchoredPosition = anchoredPosition;
            panelRoot.sizeDelta = panelSize;

            var image = panelRoot.GetComponent<Image>();
            if (image == null)
            {
                image = panelRoot.gameObject.AddComponent<Image>();
            }

            image.color = new Color(0.05f, 0.06f, 0.08f, 0.76f);
            image.raycastTarget = false;

            if (promptText == null)
            {
                promptText = panelRoot.GetComponentInChildren<Text>(true);
                if (promptText == null)
                {
                    promptText = RuntimePanelUiUtility.CreateText(panelRoot, defaultFont, string.Empty, 22, TextAnchor.MiddleCenter);
                    RuntimePanelUiUtility.StretchToParent(promptText.rectTransform, 18f, 8f, 18f, 8f);
                }
            }

            promptText.font = defaultFont;
            promptText.alignment = TextAnchor.MiddleCenter;
            promptText.color = Color.white;
            promptText.fontStyle = FontStyle.Bold;
            promptText.raycastTarget = false;

            if (promptText.GetComponent<Shadow>() == null)
            {
                var shadow = promptText.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
                shadow.effectDistance = new Vector2(1.2f, -1.2f);
            }
        }

        private static Font LoadDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
