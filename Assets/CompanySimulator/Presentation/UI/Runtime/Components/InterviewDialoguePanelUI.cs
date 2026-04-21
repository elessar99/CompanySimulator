using CompanySimulator.Features.Npcs.Runtime.Interview;
using CompanySimulator.Presentation.UI.Runtime.Common;
using UnityEngine;
using UnityEngine.UI;

namespace CompanySimulator.Presentation.UI.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class InterviewDialoguePanelUI : MonoBehaviour
    {
        [SerializeField] private InterviewSessionManager interviewSessionManager;
        [SerializeField] private Canvas rootCanvas;

        private static readonly Color ColPanel = new Color(0.063f, 0.098f, 0.169f, 0.98f);
        private static readonly Color ColSurface = new Color(0.082f, 0.125f, 0.204f, 1f);
        private static readonly Color ColText = new Color(0.933f, 0.957f, 1f, 1f);
        private static readonly Color ColMuted = new Color(0.561f, 0.639f, 0.784f, 1f);
        private static readonly Color ColBlue = new Color(0.353f, 0.627f, 1f, 1f);
        private static readonly Color ColRed = new Color(1f, 0.42f, 0.506f, 1f);

        private Font defaultFont;
        private GameObject panelRoot;
        private Text titleText;
        private Text bodyText;

        private void Awake()
        {
            interviewSessionManager ??= FindObjectOfType<InterviewSessionManager>();
            rootCanvas ??= FindObjectOfType<Canvas>();
            defaultFont = LoadDefaultFont();
            BuildUi();
        }

        private void OnEnable()
        {
            interviewSessionManager ??= FindObjectOfType<InterviewSessionManager>();
            if (interviewSessionManager != null)
            {
                interviewSessionManager.SessionChanged -= Refresh;
                interviewSessionManager.SessionChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (interviewSessionManager != null)
            {
                interviewSessionManager.SessionChanged -= Refresh;
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

            panelRoot = RuntimePanelUiUtility.CreateUiObject("InterviewDialoguePanel", hudRoot);
            var panelRect = panelRoot.GetComponent<RectTransform>();
            RuntimePanelUiUtility.ConfigureCenteredPanel(panelRect, new Vector2(680f, 280f), 0f);
            panelRoot.AddComponent<Image>().color = ColPanel;

            var title = RuntimePanelUiUtility.CreateText(panelRoot.transform, defaultFont, "İş Görüşmesi", 28, TextAnchor.UpperLeft);
            titleText = title;
            title.color = ColText;
            title.fontStyle = FontStyle.Bold;
            RuntimePanelUiUtility.StretchToParent(title.rectTransform, 24f, 210f, 24f, 24f);

            var body = RuntimePanelUiUtility.CreateText(panelRoot.transform, defaultFont, string.Empty, 20, TextAnchor.UpperLeft);
            bodyText = body;
            body.color = ColMuted;
            RuntimePanelUiUtility.StretchToParent(body.rectTransform, 24f, 96f, 24f, 74f);

            var acceptButton = RuntimePanelUiUtility.CreateButton(panelRoot.transform, defaultFont, "AcceptButton", "Kabul Et");
            acceptButton.GetComponent<Image>().color = ColBlue;
            var acceptRect = acceptButton.GetComponent<RectTransform>();
            acceptRect.anchorMin = new Vector2(0f, 0f);
            acceptRect.anchorMax = new Vector2(0.5f, 0f);
            acceptRect.pivot = new Vector2(0f, 0f);
            acceptRect.offsetMin = new Vector2(24f, 24f);
            acceptRect.offsetMax = new Vector2(-12f, 72f);
            acceptButton.onClick.AddListener(AcceptInterview);

            var rejectButton = RuntimePanelUiUtility.CreateButton(panelRoot.transform, defaultFont, "RejectButton", "Reddet");
            rejectButton.GetComponent<Image>().color = ColRed;
            var rejectRect = rejectButton.GetComponent<RectTransform>();
            rejectRect.anchorMin = new Vector2(0.5f, 0f);
            rejectRect.anchorMax = new Vector2(1f, 0f);
            rejectRect.pivot = new Vector2(1f, 0f);
            rejectRect.offsetMin = new Vector2(12f, 24f);
            rejectRect.offsetMax = new Vector2(-24f, 72f);
            rejectButton.onClick.AddListener(RejectInterview);

            panelRoot.SetActive(false);
        }

        private void Refresh()
        {
            if (panelRoot == null)
            {
                return;
            }

            var session = interviewSessionManager != null ? interviewSessionManager.CurrentSession : null;
            panelRoot.SetActive(session != null);
            if (session == null)
            {
                return;
            }

            titleText.text = "İş Görüşmesi";
            bodyText.text = $"{session.Applicant.DisplayName}: Günlük {session.CurrentSalaryOffer.Amount:N0} maaş istiyorum. Kabul ediyor musun?";
        }

        private void AcceptInterview()
        {
            var session = interviewSessionManager != null ? interviewSessionManager.CurrentSession : null;
            if (session == null)
            {
                return;
            }

            interviewSessionManager.TryHireCurrentApplicant(session.CurrentSalaryOffer);
        }

        private void RejectInterview()
        {
            interviewSessionManager?.RejectCurrentApplicant();
        }

        private Font LoadDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
