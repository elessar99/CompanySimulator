using System.Text;
using CompanySimulator.Features.Agents.Runtime.Components;
using CompanySimulator.Features.Finance.Runtime.Components;
using CompanySimulator.Features.Rivals.Runtime.Components;
using CompanySimulator.Features.Rivals.Runtime.Models;
using CompanySimulator.Features.Sectors.Runtime.Components;
using CompanySimulator.Features.Sectors.Runtime.Services;
using CompanySimulator.Presentation.UI.Runtime.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CompanySimulator.Presentation.UI.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class DebugPanelUI : MonoBehaviour
    {
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private SectorManager sectorManager;
        [SerializeField] private RivalCompanyManager rivalCompanyManager;
        [SerializeField] private AgentManager agentManager;
        [SerializeField] private SectorPanelUI sectorPanelUI;
        [SerializeField] private EmployeePanelUI employeePanelUI;
        [SerializeField] private AccountingPanelUI accountingPanelUI;
        [SerializeField] private BankPanelUI bankPanelUI;
        [SerializeField] private FinanceOverviewPanelUI financeOverviewPanelUI;
        [SerializeField] private RivalCompanyPanelUI rivalCompanyPanelUI;
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private Vector2 panelSize = new Vector2(820f, 720f);

        private Font defaultFont;
        private GameObject panelRoot;
        private RectTransform contentRoot;
        private Text pageTitleText;
        private int activeTab;
        private string lastAgentSendResult;

        private static readonly StringBuilder SharedBuilder = new StringBuilder(512);

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            economyManager ??= FindObjectOfType<EconomyManager>();
            sectorManager ??= FindObjectOfType<SectorManager>();
            rivalCompanyManager ??= FindObjectOfType<RivalCompanyManager>();
            agentManager ??= FindObjectOfType<AgentManager>();
            sectorPanelUI ??= FindObjectOfType<SectorPanelUI>();
            employeePanelUI ??= FindObjectOfType<EmployeePanelUI>();
            accountingPanelUI ??= FindObjectOfType<AccountingPanelUI>();
            bankPanelUI ??= FindObjectOfType<BankPanelUI>();
            financeOverviewPanelUI ??= FindObjectOfType<FinanceOverviewPanelUI>();
            rivalCompanyPanelUI ??= FindObjectOfType<RivalCompanyPanelUI>();
            EnsureCanvas();
            EnsureEventSystem();
            defaultFont = LoadDefaultFont();
            BuildUi();
        }

        private void OnEnable()
        {
            if (sectorManager != null)
            {
                sectorManager.DataChanged -= RefreshPage;
                sectorManager.DataChanged += RefreshPage;
            }

            if (rivalCompanyManager != null)
            {
                rivalCompanyManager.DataChanged -= RefreshPage;
                rivalCompanyManager.DataChanged += RefreshPage;
            }

            if (economyManager != null)
            {
                economyManager.DayAdvanced -= OnDayAdvanced;
                economyManager.DayAdvanced += OnDayAdvanced;
            }

            if (agentManager != null)
            {
                agentManager.DataChanged -= RefreshPage;
                agentManager.DataChanged += RefreshPage;
            }

            RefreshPage();
        }

        private void OnDisable()
        {
            if (sectorManager != null)
            {
                sectorManager.DataChanged -= RefreshPage;
            }

            if (rivalCompanyManager != null)
            {
                rivalCompanyManager.DataChanged -= RefreshPage;
            }

            if (economyManager != null)
            {
                economyManager.DayAdvanced -= OnDayAdvanced;
            }

            if (agentManager != null)
            {
                agentManager.DataChanged -= RefreshPage;
            }
        }

        private void OnDayAdvanced(int day)
        {
            RefreshPage();
        }

        public void OpenPanel()
        {
            if (sectorPanelUI != null && sectorPanelUI.IsOpen) sectorPanelUI.ClosePanel();
            if (employeePanelUI != null && employeePanelUI.IsOpen) employeePanelUI.ClosePanel();
            if (accountingPanelUI != null && accountingPanelUI.IsOpen) accountingPanelUI.ClosePanel();
            if (bankPanelUI != null && bankPanelUI.IsOpen) bankPanelUI.ClosePanel();
            if (financeOverviewPanelUI != null && financeOverviewPanelUI.IsOpen) financeOverviewPanelUI.ClosePanel();
            if (rivalCompanyPanelUI != null && rivalCompanyPanelUI.IsOpen) rivalCompanyPanelUI.ClosePanel();

            panelRoot.SetActive(true);
            RefreshPage();
        }

        public void ClosePanel()
        {
            panelRoot.SetActive(false);
        }

        private void RefreshPage()
        {
            if (contentRoot == null)
            {
                return;
            }

            RuntimePanelUiUtility.ClearChildren(contentRoot);

            if (activeTab == 0)
            {
                pageTitleText.text = "Debug — Sektör İstatistikleri";
                RenderSectorTab();
            }
            else if (activeTab == 1)
            {
                pageTitleText.text = "Debug — Şirket İstatistikleri";
                RenderRivalTab();
            }
            else
            {
                pageTitleText.text = "Debug — Oyuncuya Gelen Ajanlar";
                RenderPlayerAgentTab();
            }
        }

        private void RenderSectorTab()
        {
            if (sectorManager == null || !sectorManager.IsInitialized)
            {
                CreateInfoCard("Sektör verisi yüklenmedi.", 48f);
                return;
            }

            var sectors = sectorManager.Sectors;
            if (sectors.Count == 0)
            {
                CreateInfoCard("Tanımlı sektör yok.", 48f);
                return;
            }

            for (var i = 0; i < sectors.Count; i++)
            {
                var sectorData = sectors[i];
                var sector = sectorData.Sector;
                if (sector == null)
                {
                    continue;
                }

                var cachedCount = SectorCompetitionService.GetCachedProjectCount(sector);
                var lingeringCount = SectorCompetitionService.GetLingeringCount(sector);
                var revenueMultiplier = SectorCompetitionService.GetCachedRevenueMultiplier(sector);

                SharedBuilder.Clear();
                SharedBuilder.Append("<b>");
                SharedBuilder.Append(sector.DisplayName);
                SharedBuilder.Append("</b>");
                SharedBuilder.Append("\nToplam Proje (cache): ");
                SharedBuilder.Append(cachedCount);
                SharedBuilder.Append("  |  Oyuncu Aktif: ");
                SharedBuilder.Append(sectorData.ActiveProjectCount);
                SharedBuilder.Append("  |  Lingering: ");
                SharedBuilder.Append(lingeringCount);
                SharedBuilder.Append("\nGelir Düşme Oranı: ");
                SharedBuilder.Append((revenueMultiplier * 100f).ToString("F1"));
                SharedBuilder.Append("% (çarpan: ");
                SharedBuilder.Append(revenueMultiplier.ToString("F3"));
                SharedBuilder.Append(")");

                var text = CreateInfoCard(SharedBuilder.ToString(), 80f);
                text.supportRichText = true;
            }
        }

        private void RenderRivalTab()
        {
            if (rivalCompanyManager == null || !rivalCompanyManager.IsInitialized)
            {
                CreateInfoCard("Rakip şirket verisi yüklenmedi.", 48f);
                return;
            }

            var rivals = rivalCompanyManager.Rivals;
            if (rivals.Count == 0)
            {
                CreateInfoCard("Tanımlı rakip şirket yok.", 48f);
                return;
            }

            for (var i = 0; i < rivals.Count; i++)
            {
                RenderRivalDebugCard(rivals[i]);
            }
        }

        private void RenderRivalDebugCard(RivalCompanyRuntimeData rival)
        {
            var definition = rival.Definition;

            SharedBuilder.Clear();
            SharedBuilder.Append("<b>");
            SharedBuilder.Append(definition.DisplayName);
            SharedBuilder.Append("</b>");
            SharedBuilder.Append("  |  Bakiye: ");
            SharedBuilder.Append(rival.Balance.Amount.ToString("N0"));
            SharedBuilder.Append("  |  Aktif İş: ");
            SharedBuilder.Append(rival.ActiveJobCount);
            SharedBuilder.Append("\nSatış Çarpanı: ");
            SharedBuilder.Append(definition.SellDesireMultiplier.ToString("F2"));
            SharedBuilder.Append("  |  İş Başlatmaya: ");
            SharedBuilder.Append(rival.DaysUntilNextJobCheck);
            SharedBuilder.Append(" gün  |  İş Satmaya: ");
            SharedBuilder.Append(rival.DaysUntilNextSellCheck);
            SharedBuilder.Append(" gün");

            var jobs = definition.AvailableJobs;
            for (var j = 0; j < jobs.Count; j++)
            {
                var job = jobs[j];
                if (job == null || job.Sector == null)
                {
                    continue;
                }

                var multiplier = SectorCompetitionService.GetCachedRevenueMultiplier(job.Sector);
                var safeMultiplier = multiplier > 0f ? multiplier : 0.01f;

                var effectiveWeight = (int)System.Math.Ceiling(job.SelectionWeight * multiplier * multiplier);
                if (effectiveWeight < 1) effectiveWeight = 1;

                var effectiveSell = job.AbandonChance * definition.SellDesireMultiplier / safeMultiplier / safeMultiplier;

                SharedBuilder.Append("\n  [");
                SharedBuilder.Append(job.Sector.DisplayName);
                SharedBuilder.Append("] Ağırlık: ");
                SharedBuilder.Append(job.SelectionWeight);
                SharedBuilder.Append(" → ");
                SharedBuilder.Append(effectiveWeight);
                SharedBuilder.Append("  |  Satma: ");
                SharedBuilder.Append((effectiveSell * 100f).ToString("F1"));
                SharedBuilder.Append("%");
            }

            var activeJobs = rival.ActiveJobs;
            if (activeJobs.Count > 0)
            {
                SharedBuilder.Append("\n<b>Aktif İşler:</b>");
                for (var j = 0; j < activeJobs.Count; j++)
                {
                    var activeJob = activeJobs[j];
                    SharedBuilder.Append("\n  • ");
                    SharedBuilder.Append(activeJob.Definition.DisplayName);
                    SharedBuilder.Append(" [");
                    SharedBuilder.Append(activeJob.Sector != null ? activeJob.Sector.DisplayName : "?");
                    SharedBuilder.Append("]  Son Gelir: ");
                    SharedBuilder.Append(activeJob.LastEarnedIncome.Amount.ToString("N0"));
                    SharedBuilder.Append("  |  Ödemeye: ");
                    SharedBuilder.Append(activeJob.Definition.PayoutIntervalDays - activeJob.DaysSinceLastPayout);
                    SharedBuilder.Append(" gün");
                }
            }

            var startLog = rival.JobStartLog;
            if (startLog.Count > 0)
            {
                var startCount = startLog.Count > 5 ? 5 : startLog.Count;
                SharedBuilder.Append("\n<b>Son Başlatılan İşler:</b>");
                for (var j = startLog.Count - startCount; j < startLog.Count; j++)
                {
                    var entry = startLog[j];
                    SharedBuilder.Append("\n  + Gün ");
                    SharedBuilder.Append(entry.Day);
                    SharedBuilder.Append(": ");
                    SharedBuilder.Append(entry.JobName);
                    SharedBuilder.Append(" (Maliyet: ");
                    SharedBuilder.Append(entry.Amount.Amount.ToString("N0"));
                    SharedBuilder.Append(")");
                }
            }

            var sellLog = rival.JobSellLog;
            if (sellLog.Count > 0)
            {
                var sellCount = sellLog.Count > 5 ? 5 : sellLog.Count;
                SharedBuilder.Append("\n<b>Son Satılan İşler:</b>");
                for (var j = sellLog.Count - sellCount; j < sellLog.Count; j++)
                {
                    var entry = sellLog[j];
                    SharedBuilder.Append("\n  - Gün ");
                    SharedBuilder.Append(entry.Day);
                    SharedBuilder.Append(": ");
                    SharedBuilder.Append(entry.JobName);
                    SharedBuilder.Append(" (Gelir: ");
                    SharedBuilder.Append(entry.Amount.Amount.ToString("N0"));
                    SharedBuilder.Append(")");
                }
            }

            var totalLines = 2 + jobs.Count + activeJobs.Count;
            if (startLog.Count > 0) totalLines += 1 + System.Math.Min(5, startLog.Count);
            if (sellLog.Count > 0) totalLines += 1 + System.Math.Min(5, sellLog.Count);
            var text = CreateInfoCard(SharedBuilder.ToString(), 26f + totalLines * 24f);
            text.supportRichText = true;
        }

        private void RenderPlayerAgentTab()
        {
            if (agentManager == null)
            {
                CreateInfoCard("Ajan sistemi bulunamadı.", 48f);
                return;
            }

            var activeAgents = agentManager.PlayerTargetedAgents;
            var failedAgents = agentManager.FailedPlayerTargetedAgents;

            SharedBuilder.Clear();
            SharedBuilder.Append("<b>Oyuncuya Gönderilen Ajanlar</b>");
            SharedBuilder.Append("\nAktif: ");
            SharedBuilder.Append(activeAgents.Count);
            SharedBuilder.Append("  |  Son Başarısız: ");
            SharedBuilder.Append(failedAgents.Count);
            var headerText = CreateInfoCard(SharedBuilder.ToString(), 58f);
            headerText.supportRichText = true;

            if (activeAgents.Count > 0)
            {
                CreateInfoCard("<b>Aktif Ajanlar</b>", 32f).supportRichText = true;
                for (var i = 0; i < activeAgents.Count; i++)
                {
                    var agent = activeAgents[i];
                    SharedBuilder.Clear();
                    SharedBuilder.Append(agent.Definition.DisplayName);
                    SharedBuilder.Append(" | Gönderen: ");
                    SharedBuilder.Append(agent.SourceRival.Definition.DisplayName);
                    SharedBuilder.Append("\nSektör: ");
                    SharedBuilder.Append(agent.TargetSector.DisplayName);
                    SharedBuilder.Append(" | Kalan Gün: ");
                    SharedBuilder.Append(agent.RemainingDays);
                    SharedBuilder.Append(" | Etkilenen Proje: ");
                    SharedBuilder.Append(agent.AffectedProjects.Count);
                    SharedBuilder.Append(" | Maliyet: ");
                    SharedBuilder.Append(agent.Cost.Amount.ToString("N0"));

                    for (var j = 0; j < agent.AffectedProjects.Count; j++)
                    {
                        var project = agent.AffectedProjects[j];
                        SharedBuilder.Append("\n  • ");
                        SharedBuilder.Append(project.DisplayName);
                        SharedBuilder.Append(" [");
                        SharedBuilder.Append(project.Sector != null ? project.Sector.DisplayName : "?");
                        SharedBuilder.Append("] Gelir Düşme: %");
                        SharedBuilder.Append(((1f - agent.Definition.RevenueReductionMultiplier) * 100f).ToString("F0"));
                    }

                    var lineCount = 2 + agent.AffectedProjects.Count;
                    CreateInfoCard(SharedBuilder.ToString(), 20f + lineCount * 24f);
                }
            }

            if (failedAgents.Count > 0)
            {
                CreateInfoCard("<b>Son Başarısız Ajanlar</b>", 32f).supportRichText = true;
                for (var i = 0; i < failedAgents.Count; i++)
                {
                    var agent = failedAgents[i];
                    SharedBuilder.Clear();
                    SharedBuilder.Append(agent.Definition.DisplayName);
                    SharedBuilder.Append(" | Gönderen: ");
                    SharedBuilder.Append(agent.SourceRival.Definition.DisplayName);
                    SharedBuilder.Append(" | Sektör: ");
                    SharedBuilder.Append(agent.TargetSector.DisplayName);
                    SharedBuilder.Append(" — BAŞARISIZ");

                    var card = CreateInfoCard(SharedBuilder.ToString(), 44f);
                    card.color = new Color(1f, 0.55f, 0.55f, 1f);
                }
            }

            if (!string.IsNullOrEmpty(lastAgentSendResult))
            {
                CreateInfoCard("<b>Son Tetikleme Sonucu:</b>", 32f).supportRichText = true;
                CreateInfoCard(lastAgentSendResult, 80f);
            }

            CreateInfoCard("<b>Ajan Gönderme Tetikle</b>", 32f).supportRichText = true;

            if (rivalCompanyManager == null || !rivalCompanyManager.IsInitialized)
            {
                CreateInfoCard("Rakip şirket verisi yüklenmedi.", 48f);
                return;
            }

            var rivals = rivalCompanyManager.Rivals;
            for (var i = 0; i < rivals.Count; i++)
            {
                var rival = rivals[i];
                var definition = rival.Definition;

                SharedBuilder.Clear();
                SharedBuilder.Append(definition.DisplayName);
                SharedBuilder.Append(" | Ajan Kontrolüne: ");
                SharedBuilder.Append(rival.DaysUntilNextAgentCheck);
                SharedBuilder.Append(" gün | Şans: %");
                SharedBuilder.Append((definition.AgentSendChance * 100f).ToString("F0"));
                SharedBuilder.Append("\nOyuncu Etkisi: ");
                SharedBuilder.Append(definition.PlayerInfluenceWeight.ToString("F1"));
                SharedBuilder.Append(" | Şirket Etkisi: ");
                SharedBuilder.Append(definition.RivalInfluenceWeight.ToString("F1"));

                if (definition.RivalAgentSetup != null)
                {
                    SharedBuilder.Append(" | Katalog: ");
                    SharedBuilder.Append(definition.RivalAgentSetup.AvailableAgents.Length);
                    SharedBuilder.Append(" ajan");
                }
                else
                {
                    SharedBuilder.Append(" | Katalog: YOK");
                }

                CreateInfoCard(SharedBuilder.ToString(), 58f);

                var capturedRival = rival;
                var sendButton = CreateButton(contentRoot, "SendAgentBtn_" + definition.Id, "Ajan Gönder → " + definition.DisplayName);
                var sendRect = sendButton.GetComponent<RectTransform>();
                sendRect.sizeDelta = new Vector2(0f, 40f);
                sendButton.onClick.AddListener(() =>
                {
                    lastAgentSendResult = agentManager.ForceRivalSendAgent(capturedRival);
                    RefreshPage();
                });
            }
        }

        private void SwitchTab(int tab)
        {
            activeTab = tab;
            RefreshPage();
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
            var button = CreateButton(rootCanvas.transform, "DebugOpenButton", "Debug Panel");
            var buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 1f);
            buttonRect.anchorMax = new Vector2(0f, 1f);
            buttonRect.pivot = new Vector2(0f, 1f);
            buttonRect.anchoredPosition = new Vector2(1280f, -80f);
            buttonRect.sizeDelta = new Vector2(200f, 44f);
            button.onClick.AddListener(OpenPanel);
        }

        private void CreatePanel()
        {
            panelRoot = CreateUiObject("DebugPanel", rootCanvas.transform);
            var panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(0f, -10f);
            panelRect.sizeDelta = panelSize;

            panelRoot.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.98f);

            var headerRoot = CreateUiObject("Header", panelRoot.transform);
            var headerRect = headerRoot.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0f, 70f);
            headerRoot.AddComponent<Image>().color = new Color(0.14f, 0.18f, 0.26f, 1f);

            pageTitleText = CreateText(headerRoot.transform, "Debug Panel", 26, TextAnchor.MiddleLeft);
            RuntimePanelUiUtility.StretchToParent(pageTitleText.rectTransform, 18f, 8f, 180f, 8f);

            var closeButton = CreateButton(headerRoot.transform, "CloseButton", "×");
            var closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-14f, 0f);
            closeRect.sizeDelta = new Vector2(50f, 40f);
            closeButton.onClick.AddListener(ClosePanel);

            var tabBar = CreateUiObject("TabBar", panelRoot.transform);
            var tabBarRect = tabBar.GetComponent<RectTransform>();
            tabBarRect.anchorMin = new Vector2(0f, 1f);
            tabBarRect.anchorMax = new Vector2(1f, 1f);
            tabBarRect.pivot = new Vector2(0.5f, 1f);
            tabBarRect.anchoredPosition = new Vector2(0f, -70f);
            tabBarRect.sizeDelta = new Vector2(0f, 44f);
            tabBar.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.2f, 1f);

            var tabLayout = tabBar.AddComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 6f;
            tabLayout.childControlWidth = true;
            tabLayout.childControlHeight = true;
            tabLayout.childForceExpandWidth = true;
            tabLayout.childForceExpandHeight = true;
            tabLayout.padding = new RectOffset(8, 8, 4, 4);

            var sectorTabButton = CreateButton(tabBar.transform, "SectorTab", "Sektörler");
            sectorTabButton.onClick.AddListener(() => SwitchTab(0));

            var rivalTabButton = CreateButton(tabBar.transform, "RivalTab", "Şirketler");
            rivalTabButton.onClick.AddListener(() => SwitchTab(1));

            var agentTabButton = CreateButton(tabBar.transform, "AgentTab", "Ajanlar");
            agentTabButton.onClick.AddListener(() => SwitchTab(2));

            var scrollRoot = CreateUiObject("ScrollRoot", panelRoot.transform);
            var scrollRectTransform = scrollRoot.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0f, 0f);
            scrollRectTransform.anchorMax = new Vector2(1f, 1f);
            scrollRectTransform.offsetMin = new Vector2(16f, 16f);
            scrollRectTransform.offsetMax = new Vector2(-16f, -130f);
            scrollRoot.AddComponent<Image>().color = new Color(0.11f, 0.13f, 0.17f, 0.92f);

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

        private Text CreateInfoCard(string message, float height = 58f)
        {
            return RuntimePanelUiUtility.CreateInfoCard(contentRoot, defaultFont, message, height);
        }

        private Button CreateButton(Transform parent, string objectName, string label)
        {
            return RuntimePanelUiUtility.CreateButton(parent, defaultFont, objectName, label);
        }

        private Text CreateText(Transform parent, string value, int fontSize, TextAnchor anchor)
        {
            return RuntimePanelUiUtility.CreateText(parent, defaultFont, value, fontSize, anchor);
        }

        private GameObject CreateUiObject(string objectName, Transform parent)
        {
            return RuntimePanelUiUtility.CreateUiObject(objectName, parent);
        }

        private Font LoadDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
