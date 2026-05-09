using CompanySimulator.Features.Npcs.Runtime.Interview;
using CompanySimulator.Presentation.UI.Runtime.Common;
using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine.EventSystems;
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
        private GameObject debugPanelRoot;
        private Text debugTitleText;
        private Text debugBodyText;
        private ScrollRect debugScrollRect;
        private RectTransform debugContentRect;
        private Button debugToggleButton;
        private Button debugCloseButton;
        private InputField offerInputField;
        private Button acceptButton;
        private Button rejectButton;
        private Button submitOfferButton;
        private bool isDebugVisible = true;

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
                interviewSessionManager.NegotiationUpdated -= RefreshNegotiation;
                interviewSessionManager.NegotiationUpdated += RefreshNegotiation;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (interviewSessionManager != null)
            {
                interviewSessionManager.SessionChanged -= Refresh;
                interviewSessionManager.NegotiationUpdated -= RefreshNegotiation;
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
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 24f);
            panelRect.sizeDelta = new Vector2(920f, 220f);
            panelRoot.AddComponent<Image>().color = ColPanel;

            var title = RuntimePanelUiUtility.CreateText(panelRoot.transform, defaultFont, "İş Görüşmesi", 28, TextAnchor.UpperLeft);
            titleText = title;
            title.color = ColText;
            title.fontStyle = FontStyle.Bold;
            RuntimePanelUiUtility.StretchToParent(title.rectTransform, 24f, 154f, 180f, 24f);

            var body = RuntimePanelUiUtility.CreateText(panelRoot.transform, defaultFont, string.Empty, 20, TextAnchor.UpperLeft);
            bodyText = body;
            body.color = ColMuted;
            RuntimePanelUiUtility.StretchToParent(body.rectTransform, 24f, 96f, 24f, 64f);

            debugToggleButton = RuntimePanelUiUtility.CreateButton(panelRoot.transform, defaultFont, "DebugToggleButton", "Debug");
            debugToggleButton.GetComponent<Image>().color = ColSurface;
            var debugToggleRect = debugToggleButton.GetComponent<RectTransform>();
            debugToggleRect.anchorMin = new Vector2(1f, 1f);
            debugToggleRect.anchorMax = new Vector2(1f, 1f);
            debugToggleRect.pivot = new Vector2(1f, 1f);
            debugToggleRect.anchoredPosition = new Vector2(-24f, -24f);
            debugToggleRect.sizeDelta = new Vector2(132f, 42f);
            debugToggleButton.onClick.AddListener(ToggleDebugPanel);

            acceptButton = RuntimePanelUiUtility.CreateButton(panelRoot.transform, defaultFont, "AcceptButton", "Kabul Et");
            acceptButton.GetComponent<Image>().color = ColBlue;
            var acceptRect = acceptButton.GetComponent<RectTransform>();
            acceptRect.anchorMin = new Vector2(0f, 0f);
            acceptRect.anchorMax = new Vector2(0.5f, 0f);
            acceptRect.pivot = new Vector2(0f, 0f);
            acceptRect.offsetMin = new Vector2(24f, 24f);
            acceptRect.offsetMax = new Vector2(-12f, 72f);
            acceptButton.onClick.AddListener(AcceptInterview);

            rejectButton = RuntimePanelUiUtility.CreateButton(panelRoot.transform, defaultFont, "RejectButton", "Reddet");
            rejectButton.GetComponent<Image>().color = ColRed;
            var rejectRect = rejectButton.GetComponent<RectTransform>();
            rejectRect.anchorMin = new Vector2(0.5f, 0f);
            rejectRect.anchorMax = new Vector2(1f, 0f);
            rejectRect.pivot = new Vector2(1f, 0f);
            rejectRect.offsetMin = new Vector2(12f, 24f);
            rejectRect.offsetMax = new Vector2(-24f, 72f);
            rejectButton.onClick.AddListener(RejectInterview);

            offerInputField = RuntimePanelUiUtility.CreateInputField(panelRoot.transform, defaultFont, string.Empty);
            var offerRect = offerInputField.GetComponent<RectTransform>();
            offerRect.anchorMin = new Vector2(0f, 0f);
            offerRect.anchorMax = new Vector2(1f, 0f);
            offerRect.pivot = new Vector2(0.5f, 0f);
            offerRect.offsetMin = new Vector2(24f, 82f);
            offerRect.offsetMax = new Vector2(-216f, 132f);

            submitOfferButton = RuntimePanelUiUtility.CreateButton(panelRoot.transform, defaultFont, "SubmitOfferButton", "Teklif Ver");
            submitOfferButton.GetComponent<Image>().color = ColSurface;
            var submitRect = submitOfferButton.GetComponent<RectTransform>();
            submitRect.anchorMin = new Vector2(1f, 0f);
            submitRect.anchorMax = new Vector2(1f, 0f);
            submitRect.pivot = new Vector2(1f, 0f);
            submitRect.offsetMin = new Vector2(-180f, 82f);
            submitRect.offsetMax = new Vector2(-24f, 132f);
            submitOfferButton.onClick.AddListener(SubmitOffer);

            debugPanelRoot = RuntimePanelUiUtility.CreateUiObject("InterviewDebugPanel", hudRoot);
            var debugRect = debugPanelRoot.GetComponent<RectTransform>();
            debugRect.anchorMin = new Vector2(0.5f, 0f);
            debugRect.anchorMax = new Vector2(0.5f, 1f);
            debugRect.pivot = new Vector2(0.5f, 0.5f);
            debugRect.offsetMin = new Vector2(-520f, 256f);
            debugRect.offsetMax = new Vector2(520f, -48f);
            debugPanelRoot.AddComponent<Image>().color = new Color(0.04f, 0.06f, 0.11f, 0.97f);

            var debugHeader = RuntimePanelUiUtility.CreateUiObject("DebugHeader", debugPanelRoot.transform);
            var debugHeaderRect = debugHeader.GetComponent<RectTransform>();
            debugHeaderRect.anchorMin = new Vector2(0f, 1f);
            debugHeaderRect.anchorMax = new Vector2(1f, 1f);
            debugHeaderRect.pivot = new Vector2(0.5f, 1f);
            debugHeaderRect.anchoredPosition = Vector2.zero;
            debugHeaderRect.sizeDelta = new Vector2(0f, 54f);
            debugHeader.AddComponent<Image>().color = ColSurface;

            debugTitleText = RuntimePanelUiUtility.CreateText(debugHeader.transform, defaultFont, "Interview Debug", 24, TextAnchor.MiddleLeft);
            debugTitleText.color = ColText;
            debugTitleText.fontStyle = FontStyle.Bold;
            RuntimePanelUiUtility.StretchToParent(debugTitleText.rectTransform, 18f, 8f, 100f, 8f);

            debugCloseButton = RuntimePanelUiUtility.CreateButton(debugHeader.transform, defaultFont, "DebugCloseButton", "×");
            debugCloseButton.GetComponent<Image>().color = ColRed;
            var debugCloseRect = debugCloseButton.GetComponent<RectTransform>();
            debugCloseRect.anchorMin = new Vector2(1f, 0.5f);
            debugCloseRect.anchorMax = new Vector2(1f, 0.5f);
            debugCloseRect.pivot = new Vector2(1f, 0.5f);
            debugCloseRect.anchoredPosition = new Vector2(-12f, 0f);
            debugCloseRect.sizeDelta = new Vector2(56f, 36f);
            debugCloseButton.onClick.AddListener(ToggleDebugPanel);

            var debugScrollRoot = RuntimePanelUiUtility.CreateUiObject("DebugScrollRoot", debugPanelRoot.transform);
            var debugScrollRectTransform = debugScrollRoot.GetComponent<RectTransform>();
            debugScrollRectTransform.anchorMin = new Vector2(0f, 0f);
            debugScrollRectTransform.anchorMax = new Vector2(1f, 1f);
            debugScrollRectTransform.offsetMin = new Vector2(16f, 16f);
            debugScrollRectTransform.offsetMax = new Vector2(-16f, -68f);
            debugScrollRoot.AddComponent<Image>().color = new Color(0.06f, 0.09f, 0.15f, 1f);

            debugScrollRect = debugScrollRoot.AddComponent<ScrollRect>();
            debugScrollRect.horizontal = false;
            debugScrollRect.vertical = true;
            debugScrollRect.movementType = ScrollRect.MovementType.Clamped;
            debugScrollRect.scrollSensitivity = 24f;

            var debugViewport = RuntimePanelUiUtility.CreateUiObject("Viewport", debugScrollRoot.transform);
            var debugViewportRect = debugViewport.GetComponent<RectTransform>();
            debugViewportRect.anchorMin = Vector2.zero;
            debugViewportRect.anchorMax = Vector2.one;
            debugViewportRect.offsetMin = new Vector2(8f, 8f);
            debugViewportRect.offsetMax = new Vector2(-8f, -8f);
            debugViewport.AddComponent<RectMask2D>();
            debugViewport.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);

            var debugContent = RuntimePanelUiUtility.CreateUiObject("Content", debugViewport.transform);
            debugContentRect = debugContent.GetComponent<RectTransform>();
            debugContentRect.anchorMin = new Vector2(0f, 1f);
            debugContentRect.anchorMax = new Vector2(1f, 1f);
            debugContentRect.pivot = new Vector2(0.5f, 1f);
            debugContentRect.anchoredPosition = new Vector2(0f, -6f);
            debugContentRect.sizeDelta = new Vector2(0f, 0f);
            var debugContentFitter = debugContent.AddComponent<ContentSizeFitter>();
            debugContentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            debugContentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            debugBodyText = RuntimePanelUiUtility.CreateText(debugContent.transform, defaultFont, string.Empty, 18, TextAnchor.UpperLeft);
            debugBodyText.color = ColText;
            debugBodyText.supportRichText = true;
            debugBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            debugBodyText.verticalOverflow = VerticalWrapMode.Overflow;
            RuntimePanelUiUtility.StretchToParent(debugBodyText.rectTransform, 12f, 18f, 12f, 18f);

            var debugBodyFitter = debugBodyText.gameObject.AddComponent<ContentSizeFitter>();
            debugBodyFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            debugBodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            debugScrollRect.viewport = debugViewportRect;
            debugScrollRect.content = debugContentRect;

            panelRoot.SetActive(false);
            debugPanelRoot.SetActive(false);
        }

        private void Refresh()
        {
            if (panelRoot == null)
            {
                return;
            }

            var session = interviewSessionManager != null ? interviewSessionManager.CurrentSession : null;
            panelRoot.SetActive(session != null);
            if (session != null)
            {
                titleText.text = session.Purpose == InterviewSessionPurpose.QualityUpgrade ? "Maaş Görüşmesi" : "İş Görüşmesi";
                bodyText.text = $"{session.Applicant.DisplayName}: {BuildDialogueLine(session)}";
                RefreshControls(session);
            }

            RefreshDebugPanel();
        }

        private void AcceptInterview()
        {
            var session = interviewSessionManager != null ? interviewSessionManager.CurrentSession : null;
            if (session == null)
            {
                return;
            }

            interviewSessionManager.TryAcceptNegotiation();
        }

        private void RejectInterview()
        {
            interviewSessionManager?.RejectCurrentApplicant();
        }

        private void SubmitOffer()
        {
            var session = interviewSessionManager != null ? interviewSessionManager.CurrentSession : null;
            if (session == null || offerInputField == null)
            {
                return;
            }

            if (!long.TryParse(offerInputField.text, out var amount) || amount <= 0)
            {
                return;
            }

            interviewSessionManager.TrySubmitPlayerOffer(Money.From(amount));
        }

        private Font LoadDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private void RefreshNegotiation(InterviewSessionRuntimeData session)
        {
            Refresh();
            if (session != null && offerInputField != null)
            {
                if (string.IsNullOrWhiteSpace(offerInputField.text)
                    || session.NegotiationState == InterviewNegotiationState.WaitingForPlayerOpeningOffer)
                {
                    offerInputField.text = session.BaseExpectation.Amount.ToString();
                }
            }

            RefreshDebugPanel();
        }

        private void RefreshControls(InterviewSessionRuntimeData session)
        {
            if (session == null)
            {
                return;
            }

            var canSubmitOffer = session.NegotiationState == InterviewNegotiationState.WaitingForPlayerOpeningOffer
                || (session.NegotiationState == InterviewNegotiationState.WaitingForPlayerDecisionOnNpcOffer && !session.IsFinalDecisionStage);
            var canAccept = session.NegotiationState == InterviewNegotiationState.WaitingForPlayerDecisionOnNpcOffer
                || session.NegotiationState == InterviewNegotiationState.WaitingForPlayerDecisionOnCounterOffer;

            if (offerInputField != null)
            {
                offerInputField.gameObject.SetActive(canSubmitOffer);
                offerInputField.interactable = canSubmitOffer;
            }

            if (submitOfferButton != null)
            {
                submitOfferButton.gameObject.SetActive(canSubmitOffer);
                submitOfferButton.interactable = canSubmitOffer;
            }

            if (acceptButton != null)
            {
                acceptButton.gameObject.SetActive(canAccept);
                acceptButton.interactable = canAccept;
            }

            if (rejectButton != null)
            {
                rejectButton.gameObject.SetActive(canAccept || canSubmitOffer);
                rejectButton.interactable = canAccept || canSubmitOffer;
            }
        }

        private static string BuildDialogueLine(InterviewSessionRuntimeData session)
        {
            if (session == null)
            {
                return string.Empty;
            }

            switch (session.LatestDialogue.Intent)
            {
                case InterviewDialogueIntent.NpcRequestsPlayerOffer:
                    return "Bu pozisyon için nasıl bir günlük maaş düşünüyorsun?";
                case InterviewDialogueIntent.NpcCounterOffers:
                    return session.IsFinalDecisionStage
                        ? $"Son teklifim günlük {session.CurrentSalaryOffer.Amount:N0}. Kabul edersen anlaşırız, etmezsen görüşmeyi bitiririz."
                        : $"Bu rakam düşük kaldı. Günlük {session.CurrentSalaryOffer.Amount:N0} olursa anlaşabiliriz.";
                case InterviewDialogueIntent.NpcAcceptsOffer:
                    return $"Günlük {session.CurrentSalaryOffer.Amount:N0} ile anlaşabiliriz.";
                case InterviewDialogueIntent.NpcHardRejectsOffer:
                    return "Bu teklif benim için çok düşük. Görüşmeyi burada bitirelim.";
                case InterviewDialogueIntent.NpcSoftRejectsOffer:
                    return "Bu teklif biraz düşük kaldı. Biraz daha yükseltirsen düşünebilirim.";
                case InterviewDialogueIntent.PlayerAcceptedNpcOffer:
                    return $"Günlük {session.CurrentSalaryOffer.Amount:N0} teklifimi kabul ettin.";
                case InterviewDialogueIntent.PlayerRejectedNpcOffer:
                    return "Teklifimi kabul etmediğin için görüşmeyi bitiriyorum.";
                case InterviewDialogueIntent.NpcOpeningOffer:
                default:
                    return $"Günlük {session.CurrentSalaryOffer.Amount:N0} maaş istiyorum. Kabul ediyor musun?";
            }
        }

        private void ToggleDebugPanel()
        {
            isDebugVisible = !isDebugVisible;
            RefreshDebugPanel();
        }

        private void RefreshDebugPanel()
        {
            if (debugPanelRoot == null)
            {
                return;
            }

            var hasDebugData = interviewSessionManager != null && !string.IsNullOrWhiteSpace(interviewSessionManager.GetInterviewDebugSnapshot());
            debugPanelRoot.SetActive(isDebugVisible && hasDebugData);

            if (debugBodyText != null && interviewSessionManager != null)
            {
                debugBodyText.text = interviewSessionManager.GetInterviewDebugSnapshot();
            }

            RefreshDebugLayout();

            if (debugToggleButton != null)
            {
                var toggleText = debugToggleButton.GetComponentInChildren<Text>();
                if (toggleText != null)
                {
                    toggleText.text = isDebugVisible ? "Debug Gizle" : "Debug Göster";
                }
            }
        }

        private void RefreshDebugLayout()
        {
            if (debugPanelRoot == null || !debugPanelRoot.activeSelf || debugBodyText == null || debugContentRect == null || debugScrollRect == null)
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(debugBodyText.rectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(debugContentRect);
            Canvas.ForceUpdateCanvases();
            debugScrollRect.verticalNormalizedPosition = 1f;
            EventSystem.current?.SetSelectedGameObject(null);
        }
    }
}
