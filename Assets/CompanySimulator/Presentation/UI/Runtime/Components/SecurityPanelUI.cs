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

        private Font defaultFont;
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
            var searchButton = CreateButton(contentRoot, "SearchAgentsButton", $"Ajan Ara (Maliyet: {cost.Amount:N0})");
            searchButton.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 56f);
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

            searchFeedbackText = CreateInfoCard("Şirkette gizli ajan olup olmadığını kontrol etmek için arama yap.", 64f);
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

            var summaryMessage = $"Tespit Edilen Aktif Ajan: {detectedCount}\n" +
                                 $"Etkilenen İş Sayısı: {totalAffectedProjects}\n" +
                                 $"Toplam Tahmini Gelir Kaybı: {totalRevenueLoss:N0} / döngü";
            CreateInfoCard(summaryMessage, 88f);
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
            var senderName = agent.SourceRival != null && agent.SourceRival.Definition != null
                ? agent.SourceRival.Definition.DisplayName
                : "Bilinmiyor";
            var sectorName = agent.TargetSector != null
                ? agent.TargetSector.DisplayName
                : "Bilinmiyor";
            var currentDay = economyManager != null ? economyManager.CurrentDay : 0;
            var elapsedDays = currentDay > agent.DeployDay ? currentDay - agent.DeployDay : 0;
            var expiryStatus = agent.IsExpired ? " (Süresi Dolmuş)" : "";
            var reductionPercent = (1f - definition.RevenueReductionMultiplier) * 100f;

            var headerText = $"Ajan: {definition.DisplayName} | Durum: Tespit Edildi{expiryStatus}\n" +
                             $"Gönderen: {senderName} | Hedef Sektör: {sectorName}\n" +
                             $"Yerleşim: Gün {agent.DeployDay} | Geçen Süre: {elapsedDays} gün | Kalan: {agent.RemainingDays} gün\n" +
                             $"Gelir Azaltma: %{reductionPercent:F0} | Maks. Sabotaj: {definition.MaxSimultaneousSabotage}";

            CreateInfoCard(headerText, 112f);

            var affectedProjects = agent.AffectedProjects;
            if (affectedProjects.Count == 0)
            {
                CreateInfoCard("  Bu ajan henüz hiçbir işi etkilememiş.", 48f);
                return;
            }

            for (var j = 0; j < affectedProjects.Count; j++)
            {
                var project = affectedProjects[j];
                var projectSectorName = project.Sector != null ? project.Sector.DisplayName : "-";
                var cycleRevenue = project.CycleRevenue.Amount;
                var revenueLoss = (long)(cycleRevenue * (1f - definition.RevenueReductionMultiplier));

                var projectDetail = $"  → {project.DisplayName} ({projectSectorName})\n" +
                                    $"     Döngü Geliri: {cycleRevenue:N0} | Kayıp: -{revenueLoss:N0}";
                CreateInfoCard(projectDetail, 68f);
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
                CreateInfoCard(message, 88f);
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

            if (rootCanvas != null)
            {
                return;
            }

            var canvasObject = new GameObject("MainCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            rootCanvas = canvasObject.GetComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
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
            var button = CreateButton(rootCanvas.transform, "SecurityOpenButton", "Güvenlik");
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
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(0f, -10f);
            panelRect.sizeDelta = panelSize;

            var background = panelRoot.AddComponent<Image>();
            background.color = new Color(0.1f, 0.12f, 0.16f, 0.98f);

            var headerRoot = CreateUiObject("Header", panelRoot.transform);
            var headerRect = headerRoot.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0f, 70f);
            headerRoot.AddComponent<Image>().color = new Color(0.17f, 0.21f, 0.29f, 1f);

            pageTitleText = CreateText(headerRoot.transform, "Güvenlik", 28, TextAnchor.MiddleLeft);
            StretchToParent(pageTitleText.rectTransform, 18f, 8f, 140f, 8f);

            var closeButton = CreateButton(headerRoot.transform, "CloseButton", "×");
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
            scrollRoot.AddComponent<Image>().color = new Color(0.13f, 0.15f, 0.19f, 0.92f);

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
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
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
            var titleText = CreateText(contentRoot, title, 24, TextAnchor.MiddleLeft);
            titleText.rectTransform.sizeDelta = new Vector2(0f, 36f);
            titleText.color = new Color(0.94f, 0.94f, 0.98f, 1f);
        }

        private Text CreateInfoCard(string message, float height = 58f)
        {
            return RuntimePanelUiUtility.CreateInfoCard(contentRoot, defaultFont, message, height);
        }

        private GameObject CreateUiObject(string objectName, Transform parent)
        {
            return RuntimePanelUiUtility.CreateUiObject(objectName, parent);
        }

        private Button CreateButton(Transform parent, string objectName, string label)
        {
            return RuntimePanelUiUtility.CreateButton(parent, defaultFont, objectName, label);
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

        private Font LoadDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
