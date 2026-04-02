using System.Collections.Generic;
using CompanySimulator.Features.Agents.Runtime.Components;
using CompanySimulator.Features.Agents.Runtime.Models;
using CompanySimulator.Features.Finance.Runtime.Components;
using CompanySimulator.Features.Finance.Runtime.Models;
using CompanySimulator.Presentation.UI.Runtime.Common;
using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CompanySimulator.Presentation.UI.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class SecurityPanelUI : MonoBehaviour
    {
        [SerializeField] private AgentManager agentManager;
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private SectorPanelUI sectorPanelUI;
        [SerializeField] private EmployeePanelUI employeePanelUI;
        [SerializeField] private AccountingPanelUI accountingPanelUI;
        [SerializeField] private BankPanelUI bankPanelUI;
        [SerializeField] private FinanceOverviewPanelUI financeOverviewPanelUI;
        [SerializeField] private RivalCompanyPanelUI rivalCompanyPanelUI;
        [SerializeField] private DebugPanelUI debugPanelUI;
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private Vector2 panelSize = new Vector2(780f, 720f);
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
        private static readonly Color ColGreen = new Color(0.263f, 0.839f, 0.561f, 1f);
        private static readonly Color ColRed = new Color(1f, 0.42f, 0.506f, 1f);
        private static readonly Color ColPurple = new Color(0.62f, 0.46f, 1f, 1f);

        private Font defaultFont;
        private Sprite roundedSprite;
        private GameObject panelRoot;
        private RectTransform contentRoot;
        private Text pageTitleText;
        private Text searchFeedbackText;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            agentManager ??= FindObjectOfType<AgentManager>();
            economyManager ??= FindObjectOfType<EconomyManager>();
            sectorPanelUI ??= FindObjectOfType<SectorPanelUI>();
            employeePanelUI ??= FindObjectOfType<EmployeePanelUI>();
            accountingPanelUI ??= FindObjectOfType<AccountingPanelUI>();
            bankPanelUI ??= FindObjectOfType<BankPanelUI>();
            financeOverviewPanelUI ??= FindObjectOfType<FinanceOverviewPanelUI>();
            rivalCompanyPanelUI ??= FindObjectOfType<RivalCompanyPanelUI>();
            debugPanelUI ??= FindObjectOfType<DebugPanelUI>();
            EnsureCanvas();
            EnsureEventSystem();
            defaultFont = LoadDefaultFont();
            roundedSprite = LoadRoundedSprite();
            BuildUi();
        }

        private void OnEnable()
        {
            if (agentManager != null)
            {
                agentManager.DataChanged -= RefreshPage;
                agentManager.DataChanged += RefreshPage;
            }

            if (economyManager != null)
            {
                economyManager.DayAdvanced -= HandleDayAdvanced;
                economyManager.DayAdvanced += HandleDayAdvanced;
            }

            RefreshPage();
        }

        private void OnDisable()
        {
            if (agentManager != null)
            {
                agentManager.DataChanged -= RefreshPage;
            }

            if (economyManager != null)
            {
                economyManager.DayAdvanced -= HandleDayAdvanced;
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

            if (accountingPanelUI != null && accountingPanelUI.IsOpen)
            {
                accountingPanelUI.ClosePanel();
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

            panelRoot.SetActive(true);
            RuntimePanelUiUtility.BringToFront(panelRoot);
            RefreshPage();
        }

        public void ClosePanel()
        {
            panelRoot.SetActive(false);
        }

        private void HandleDayAdvanced(int _)
        {
            if (IsOpen)
            {
                RefreshPage();
            }
        }

        private void RefreshPage()
        {
            if (contentRoot == null)
            {
                return;
            }

            ClearChildren(contentRoot);
            pageTitleText.text = "Güvenlik";
            searchFeedbackText = null;

            RenderSearchSection();
            RenderSummarySection();
            RenderActiveAgentDetails();
            RenderDismissedAgentHistory();
        }

        private void RenderSearchSection()
        {
            CreateSectionTitle("Ajan Arama");

            if (agentManager == null)
            {
                CreateInfoCard("Ajan sistemi sahnede bulunamadı.");
                return;
            }

            var cost = agentManager.GetAgentSearchCost();
            var searchButton = CreateStyledButton(contentRoot, "SearchAgentsButton", $"Ajan Ara (Maliyet: {cost.Amount:N0})", ColBlue, Blend(ColBlue, ColCyan, 0.28f), Darken(ColBlue, 0.22f), ColText, TextAnchor.MiddleCenter);
            var searchLayout = searchButton.gameObject.AddComponent<LayoutElement>();
            searchLayout.preferredHeight = 52f;
            searchLayout.minHeight = 52f;
            searchButton.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 52f);
            searchButton.onClick.AddListener(() =>
            {
                if (agentManager != null)
                {
                    var result = agentManager.SearchForAgents();
                    if (searchFeedbackText != null)
                    {
                        searchFeedbackText.text = result;
                    }

                    RefreshPage();
                }
            });

            searchFeedbackText = CreateInfoCard("Şirkette gizli ajan olup olmadığını kontrol etmek için arama yap.", 72f);
        }

        private void RenderSummarySection()
        {
            if (agentManager == null)
            {
                return;
            }

            var activeAgents = agentManager.PlayerTargetedAgents;
            var detectedCount = 0;
            var totalRevenueLoss = 0L;
            var totalAffectedProjects = 0;

            for (var i = 0; i < activeAgents.Count; i++)
            {
                var agent = activeAgents[i];
                if (!agent.IsActive || !agent.IsDetected)
                {
                    continue;
                }

                detectedCount++;
                totalAffectedProjects += agent.AffectedProjects.Count;
                totalRevenueLoss += CalculateAgentRevenueLoss(agent);
            }

            if (detectedCount == 0)
            {
                CreateInfoCard("Henüz ajan tespit edilmedi.\nArama yaparak şirketini kontrol et.", 64f);
                return;
            }

            CreateSectionTitle("Özet");

            var summaryGridHost = CreateUiObject("SecuritySummaryGrid", contentRoot);
            var summaryGrid = summaryGridHost.AddComponent<GridLayoutGroup>();
            summaryGrid.cellSize = new Vector2(220f, 96f);
            summaryGrid.spacing = new Vector2(12f, 12f);
            summaryGrid.padding = new RectOffset(0, 0, 0, 0);
            summaryGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            summaryGrid.constraintCount = 3;
            summaryGrid.childAlignment = TextAnchor.UpperLeft;
            var summaryFitter = summaryGridHost.AddComponent<ContentSizeFitter>();
            summaryFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            summaryFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateMetricCard(summaryGridHost.transform, "Tespit Edilen", detectedCount.ToString(), "Aktif Ajan", ColBlue);
            CreateMetricCard(summaryGridHost.transform, "Etkilenen İş", totalAffectedProjects.ToString(), "Sabotaj Altında", ColGold);
            CreateMetricCard(summaryGridHost.transform, "Gelir Kaybı", totalRevenueLoss.ToString("N0"), "Döngü Başına", ColRed);
        }

        private void RenderActiveAgentDetails()
        {
            if (agentManager == null)
            {
                return;
            }

            var activeAgents = agentManager.PlayerTargetedAgents;
            var hasDetected = false;

            for (var i = 0; i < activeAgents.Count; i++)
            {
                if (activeAgents[i].IsActive && activeAgents[i].IsDetected)
                {
                    hasDetected = true;
                    break;
                }
            }

            if (!hasDetected)
            {
                return;
            }

            CreateSectionTitle("Tespit Edilen Ajanlar");

            for (var i = 0; i < activeAgents.Count; i++)
            {
                var agent = activeAgents[i];
                if (!agent.IsActive || !agent.IsDetected)
                {
                    continue;
                }

                RenderAgentCard(agent);
            }
        }

        private void RenderAgentCard(PlayerTargetedAgentRuntimeData agent)
        {
            var definition = agent.Definition;
            var affectedProjects = agent.AffectedProjects;
            var senderName = agent.SourceRival != null && agent.SourceRival.Definition != null
                ? agent.SourceRival.Definition.DisplayName
                : "Bilinmiyor";
            var sectorName = agent.TargetSector != null
                ? agent.TargetSector.DisplayName
                : "Bilinmiyor";
            var currentDay = economyManager != null ? economyManager.CurrentDay : 0;
            var elapsedDays = currentDay > agent.DeployDay ? currentDay - agent.DeployDay : 0;
            var reductionPercent = (1f - definition.RevenueReductionMultiplier) * 100f;

            var cardHeight = 172f + Mathf.Max(0, affectedProjects.Count - 1) * 56f;
            var card = CreateSurface(contentRoot, "DetectedAgent_" + definition.Id, cardHeight, ColPanel);
            AddHoverEffect(card, ColPanel, Blend(ColPanel, ColRed, 0.16f));
            CreateAccentBar(card.transform, ColRed);

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

            var title = CreateText(topRow.transform, definition.DisplayName, 18, TextAnchor.MiddleLeft);
            title.color = ColText;
            title.fontStyle = FontStyle.Bold;
            title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            CreateTag(topRow.transform, agent.IsExpired ? "Süresi Dolmuş" : "Tespit Edildi", new Color(ColRed.r, ColRed.g, ColRed.b, 0.18f), ColRed, 13);

            var detail = CreateText(content.transform, $"Gönderen: {senderName} | Hedef: {sectorName}\nYerleşim: Gün {agent.DeployDay} | Geçen: {elapsedDays} gün | Kalan: {agent.RemainingDays} gün\nGelir Azaltma: %{reductionPercent:F0} | Maks. Sabotaj: {definition.MaxSimultaneousSabotage}", 13, TextAnchor.MiddleLeft);
            detail.color = ColMuted;
            detail.gameObject.AddComponent<LayoutElement>().preferredHeight = 54f;

            if (affectedProjects.Count == 0)
            {
                var emptyState = CreateText(content.transform, "Bu ajan henüz hiçbir işi etkilememiş.", 13, TextAnchor.MiddleLeft);
                emptyState.color = ColMuted;
                emptyState.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
                return;
            }

            for (var j = 0; j < affectedProjects.Count; j++)
            {
                var project = affectedProjects[j];
                var projectSectorName = project.Sector != null ? project.Sector.DisplayName : "-";
                var cycleRevenue = project.CycleRevenue.Amount;
                var revenueLoss = (long)(cycleRevenue * (1f - definition.RevenueReductionMultiplier));

                var projectDetail = CreateSurface(content.transform, "AffectedProject_" + j, 56f, ColSurfaceAlt);
                var projectText = CreateText(projectDetail.transform, $"{project.DisplayName} ({projectSectorName})\nDöngü Geliri: {cycleRevenue:N0} | Kayıp: -{revenueLoss:N0}", 13, TextAnchor.MiddleLeft);
                projectText.color = ColMuted;
                StretchToParent(projectText.rectTransform, 12f, 8f, 12f, 8f);
            }
        }

        private void RenderDismissedAgentHistory()
        {
            if (agentManager == null)
            {
                return;
            }

            var dismissed = agentManager.DismissedPlayerAgents;
            if (dismissed.Count == 0)
            {
                return;
            }

            CreateSectionTitle("Son Kovulan Ajanlar");

            for (var i = 0; i < dismissed.Count; i++)
            {
                var agent = dismissed[i];
                var definition = agent.Definition;
                var senderName = agent.SourceRival != null && agent.SourceRival.Definition != null
                    ? agent.SourceRival.Definition.DisplayName
                    : "Bilinmiyor";
                var sectorName = agent.TargetSector != null
                    ? agent.TargetSector.DisplayName
                    : "Bilinmiyor";
                var reductionPercent = (1f - definition.RevenueReductionMultiplier) * 100f;

                var message = $"[Kovuldu] {definition.DisplayName}\n" +
                              $"Gönderen: {senderName} | Sektör: {sectorName}\n" +
                              $"Gelir Azaltma: %{reductionPercent:F0} | Yerleşim: Gün {agent.DeployDay}";
                CreateInfoCard(message, 92f);
            }
        }

        private long CalculateAgentRevenueLoss(PlayerTargetedAgentRuntimeData agent)
        {
            var loss = 0L;
            var multiplier = 1f - agent.Definition.RevenueReductionMultiplier;
            var affectedProjects = agent.AffectedProjects;

            for (var i = 0; i < affectedProjects.Count; i++)
            {
                loss += (long)(affectedProjects[i].CycleRevenue.Amount * multiplier);
            }

            return loss;
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
            var button = CreateStyledButton(rootCanvas.transform, "SecurityOpenButton", "Güvenlik", ColSurface, Blend(ColSurface, ColBlue, 0.25f), Darken(ColSurface, 0.16f), ColText, TextAnchor.MiddleCenter);
            var buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 1f);
            buttonRect.anchorMax = new Vector2(0f, 1f);
            buttonRect.pivot = new Vector2(0f, 1f);
            buttonRect.anchoredPosition = new Vector2(1490f, -80f);
            buttonRect.sizeDelta = new Vector2(180f, 44f);
            button.onClick.AddListener(OpenPanel);
        }

        private void CreatePanel()
        {
            panelRoot = CreateUiObject("SecurityPanel", rootCanvas.transform);
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
            var badgeText = CreateText(badge.transform, "SEC", 16, TextAnchor.MiddleCenter);
            badgeText.color = ColCyan;
            badgeText.fontStyle = FontStyle.Bold;
            StretchToParent(badgeText.rectTransform, 0f, 0f, 0f, 0f);

            pageTitleText = CreateText(headerRoot.transform, "Güvenlik", 28, TextAnchor.MiddleLeft);
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
            layout.spacing = 12f;
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
            StretchToParent(text.rectTransform, 14f, 8f, 14f, 8f);
            return text;
        }

        private GameObject CreateUiObject(string objectName, Transform parent)
        {
            return RuntimePanelUiUtility.CreateUiObject(objectName, parent);
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

        private void StretchToParent(RectTransform rectTransform, float left, float bottom, float right, float top)
        {
            RuntimePanelUiUtility.StretchToParent(rectTransform, left, bottom, right, top);
        }

        private void ClearChildren(RectTransform parent)
        {
            RuntimePanelUiUtility.ClearChildren(parent);
        }

        private void CreateMetricCard(Transform parent, string title, string value, string badge, Color accent)
        {
            var card = CreateSurface(parent, title.Replace(' ', '_') + "Metric", 96f, ColSurface);
            var rect = card.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(220f, 96f);
            var layout = card.GetComponent<LayoutElement>();
            layout.preferredWidth = 220f;
            layout.minWidth = 220f;
            CreateAccentBar(card.transform, accent);

            var content = CreateStretchContainer(card.transform, "Content", 12f, 12f, 12f, 12f);
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 0, 0, 0);
            contentLayout.spacing = 4f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var titleText = CreateText(content.transform, title, 14, TextAnchor.MiddleLeft);
            titleText.color = ColMuted;
            titleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

            var valueText = CreateText(content.transform, value, 24, TextAnchor.MiddleLeft);
            valueText.color = ColText;
            valueText.fontStyle = FontStyle.Bold;
            valueText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

            CreateFlexibleSpacer(content.transform);
            CreateTag(content.transform, badge, new Color(accent.r, accent.g, accent.b, 0.18f), accent, 13);
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
