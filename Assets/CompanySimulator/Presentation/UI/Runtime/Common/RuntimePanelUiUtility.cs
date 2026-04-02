using UnityEngine;
using UnityEngine.UI;
using CompanySimulator.Features.Employees.Runtime.Definitions;

namespace CompanySimulator.Presentation.UI.Runtime.Common
{
    public static class RuntimePanelUiUtility
    {
        public static GameObject CreateUiObject(string objectName, Transform parent)
        {
            var uiObject = new GameObject(objectName, typeof(RectTransform));
            uiObject.transform.SetParent(parent, false);
            return uiObject;
        }

        public static Text CreateText(Transform parent, Font font, string value, int fontSize, TextAnchor anchor)
        {
            var textObject = CreateUiObject("Text", parent);
            var text = textObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        public static Button CreateButton(Transform parent, Font font, string objectName, string label)
        {
            var buttonObject = CreateUiObject(objectName, parent);
            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.23f, 0.3f, 0.42f, 1f);

            var button = buttonObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(0.23f, 0.3f, 0.42f, 1f);
            colors.highlightedColor = new Color(0.3f, 0.38f, 0.52f, 1f);
            colors.pressedColor = new Color(0.18f, 0.24f, 0.35f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);
            button.colors = colors;

            var text = CreateText(buttonObject.transform, font, label, 22, TextAnchor.MiddleLeft);
            StretchToParent(text.rectTransform, 16f, 8f, 16f, 8f);
            return button;
        }

        public static Text CreateInfoCard(Transform parent, Font font, string message, float height = 58f)
        {
            var infoRoot = CreateUiObject("InfoCard", parent);
            var rect = infoRoot.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, height);

            var image = infoRoot.AddComponent<Image>();
            image.color = new Color(0.18f, 0.2f, 0.25f, 1f);

            var text = CreateText(infoRoot.transform, font, message, 20, TextAnchor.MiddleLeft);
            StretchToParent(text.rectTransform, 14f, 6f, 14f, 6f);
            return text;
        }

        public static InputField CreateInputField(Transform parent, Font font, string initialValue)
        {
            var inputObject = CreateUiObject("InputField", parent);
            var image = inputObject.AddComponent<Image>();
            image.color = new Color(0.1f, 0.12f, 0.16f, 1f);

            var inputField = inputObject.AddComponent<InputField>();
            inputField.contentType = InputField.ContentType.IntegerNumber;
            inputField.lineType = InputField.LineType.SingleLine;
            inputField.text = initialValue;

            var placeholder = CreateText(inputObject.transform, font, "Bütçe", 18, TextAnchor.MiddleLeft);
            placeholder.color = new Color(1f, 1f, 1f, 0.35f);
            StretchToParent(placeholder.rectTransform, 10f, 6f, 10f, 6f);

            var text = CreateText(inputObject.transform, font, initialValue, 18, TextAnchor.MiddleLeft);
            StretchToParent(text.rectTransform, 10f, 6f, 10f, 6f);

            inputField.placeholder = placeholder;
            inputField.textComponent = text;
            return inputField;
        }

        public static void StretchToParent(RectTransform rectTransform, float left, float bottom, float right, float top)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(left, bottom);
            rectTransform.offsetMax = new Vector2(-right, -top);
        }

        public static void ClearChildren(RectTransform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(parent.GetChild(i).gameObject);
            }
        }

        public static void EnsureResponsiveCanvasScaler(Canvas canvas)
        {
            if (canvas == null)
            {
                return;
            }

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
        }

        public static void ConfigureCenteredPanel(RectTransform rectTransform, Vector2 panelSize, float verticalOffset)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0f, -verticalOffset);
            rectTransform.sizeDelta = panelSize;
        }

        public static void BringToFront(GameObject panelRoot)
        {
            if (panelRoot == null)
            {
                return;
            }

            panelRoot.transform.SetAsLastSibling();
        }

        public static string GetEmployeeQualityLabel(EmployeeQualityTier qualityTier)
        {
            switch (qualityTier)
            {
                case EmployeeQualityTier.Kotu:
                    return "Kötü";
                case EmployeeQualityTier.Ortalama:
                    return "Ortalama";
                case EmployeeQualityTier.Iyi:
                    return "İyi";
                default:
                    return "Profesyonel";
            }
        }
    }
}
