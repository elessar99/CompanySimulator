using System.Text;
using CompanySimulator.Features.Agents.Runtime.Components;
using CompanySimulator.Features.Agents.Runtime.Definitions;
using CompanySimulator.Features.Rivals.Runtime.Components;
using CompanySimulator.Features.Rivals.Runtime.Models;
using CompanySimulator.Features.Sectors.Runtime.Definitions;
using CompanySimulator.Presentation.UI.Runtime.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CompanySimulator.Presentation.UI.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class RivalCompanyPanelUI : MonoBehaviour
    {
        [SerializeField] private RivalCompanyManager rivalCompanyManager;
        [SerializeField] private AgentManager agentManager;
        [SerializeField] private SectorPanelUI sectorPanelUI;
        [SerializeField] private EmployeePanelUI employeePanelUI;
        [SerializeField] private AccountingPanelUI accountingPanelUI;
        [SerializeField] private BankPanelUI bankPanelUI;
        [SerializeField] private FinanceOverviewPanelUI financeOverviewPanelUI;
        [SerializeField] private DebugPanelUI debugPanelUI;
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private Vector2 panelSize = new Vector2(760f, 720f);

        private Font defaultFont;
        private GameObject panelRoot;
        private RectTransform contentRoot;
        private Text pageTitleText;

        private enum PageState
        {
            RivalList,
            RivalSectorAgents
        }

        private PageState currentPage = PageState.RivalList;
        private RivalCompanyRuntimeData selectedRival;
        private SectorDefinition selectedSector;

        private static readonly StringBuilder SharedBuilder = new StringBuilder(256);

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            rivalCompanyManager ??= FindObjectOfType<RivalCompanyManager>();
            if (rivalCompanyManager == null)
            {
                rivalCompanyManager = new GameObject("RivalCompanyManager", typeof(RivalCompanyManager))
                    .GetComponent<RivalCompanyManager>();
            }

            agentManager ??= FindObjectOfType<AgentManager>();
            if (agentManager == null)
            {
                agentManager = new GameObject("AgentManager", typeof(AgentManager))
                    .GetComponent<AgentManager>();
            }

            sectorPanelUI ??= FindObjectOfType<SectorPanelUI>();
            employeePanelUI ??= FindObjectOfType<EmployeePanelUI>();
            accountingPanelUI ??= FindObjectOfType<AccountingPanelUI>();
            bankPanelUI ??= FindObjectOfType<BankPanelUI>();
            financeOverviewPanelUI ??= FindObjectOfType<FinanceOverviewPanelUI>();
            debugPanelUI ??= FindObjectOfType<DebugPanelUI>();
            EnsureCanvas();
            EnsureEventSystem();
            defaultFont = LoadDefaultFont();
            BuildUi();
        }

        private void OnEnable()
        {
            if (rivalCompanyManager != null)
            {
                rivalCompanyManager.DataChanged -= RefreshPage;
                rivalCompanyManager.DataChanged += RefreshPage;
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
            if (rivalCompanyManager != null)
            {
                rivalCompanyManager.DataChanged -= RefreshPage;
            }

            if (agentManager != null)
            {
                agentManager.DataChanged -= RefreshPage;
            }
        }

        public void OpenPanel()
        {
            if (sectorPanelUI != null && sectorPanelUI.IsOpen) sectorPanelUI.ClosePanel();
            if (employeePanelUI != null && employeePanelUI.IsOpen) employeePanelUI.ClosePanel();
            if (accountingPanelUI != null && accountingPanelUI.IsOpen) accountingPanelUI.ClosePanel();
            if (bankPanelUI != null && bankPanelUI.IsOpen) bankPanelUI.ClosePanel();
            if (financeOverviewPanelUI != null && financeOverviewPanelUI.IsOpen) financeOverviewPanelUI.ClosePanel();
            if (debugPanelUI != null && debugPanelUI.IsOpen) debugPanelUI.ClosePanel();

            currentPage = PageState.RivalList;
            selectedRival = null;
            selectedSector = null;
            panelRoot.SetActive(true);
            RefreshPage();
        }

        public void ClosePanel()
        {
            panelRoot.SetActive(false);
        }

        private void RefreshPage()
        {
            if (contentRoot == null || rivalCompanyManager == null)
            {
                return;
            }

            if (!rivalCompanyManager.IsInitialized)
            {
                rivalCompanyManager.Initialize();
            }

            RuntimePanelUiUtility.ClearChildren(contentRoot);

            switch (currentPage)
            {
                case PageState.RivalList:
                    RenderRivalListPage();
                    break;
                case PageState.RivalSectorAgents:
                    RenderRivalSectorAgentsPage();
                    break;
            }
        }

        private void RenderRivalListPage()
        {
            pageTitleText.text = "Rakip Şirketler";

            var rivals = rivalCompanyManager.Rivals;
            if (rivals.Count == 0)
            {
                CreateInfoCard("Henüz tanımlı rakip şirket bulunmuyor.", 58f);
                return;
            }

            for (var i = 0; i < rivals.Count; i++)
            {
                RenderRivalCard(rivals[i]);
            }
        }

        private void RenderRivalCard(RivalCompanyRuntimeData rival)
        {
            SharedBuilder.Clear();
            SharedBuilder.Append(rival.Definition.DisplayName);
            SharedBuilder.Append("\nBakiye: ");
            SharedBuilder.Append(rival.Balance.Amount.ToString("N0"));
            SharedBuilder.Append("\nŞirket Değeri: ");
            SharedBuilder.Append(rival.CompanyValue.Amount.ToString("N0"));
            SharedBuilder.Append("\nAktif İş Sayısı: ");
            SharedBuilder.Append(rival.ActiveJobCount);

            CreateInfoCard(SharedBuilder.ToString(), 110f);

            var activeJobs = rival.ActiveJobs;
            var sectorJobCounts = new System.Collections.Generic.Dictionary<SectorDefinition, int>(4);
            for (var i = 0; i < activeJobs.Count; i++)
            {
                var sector = activeJobs[i].Sector;
                if (sector == null) continue;
                sectorJobCounts.TryGetValue(sector, out var count);
                sectorJobCounts[sector] = count + 1;
            }

            var sectors = rival.OperatingSectors;
            for (var i = 0; i < sectors.Count; i++)
            {
                var sector = sectors[i];
                sectorJobCounts.TryGetValue(sector, out var jobCount);
                var agentCount = agentManager != null ? agentManager.GetActiveAgentCountForRivalSector(rival, sector) : 0;

                SharedBuilder.Clear();
                SharedBuilder.Append(sector.DisplayName);
                SharedBuilder.Append(" - Aktif İş: ");
                SharedBuilder.Append(jobCount);
                if (agentCount > 0)
                {
                    SharedBuilder.Append(" | Ajan: ");
                    SharedBuilder.Append(agentCount);
                }

                var capturedRival = rival;
                var capturedSector = sector;
                var sectorButton = CreateButton(contentRoot, "SectorBtn_" + sector.Id, SharedBuilder.ToString());
                var sectorBtnRect = sectorButton.GetComponent<RectTransform>();
                sectorBtnRect.sizeDelta = new Vector2(0f, 44f);
                sectorButton.onClick.AddListener(() => NavigateToSectorAgents(capturedRival, capturedSector));
            }

            if (sectors.Count == 0)
            {
                CreateInfoCard("  Henüz aktif sektör yok.", 36f);
            }
        }

        private void NavigateToSectorAgents(RivalCompanyRuntimeData rival, SectorDefinition sector)
        {
            selectedRival = rival;
            selectedSector = sector;
            currentPage = PageState.RivalSectorAgents;
            RefreshPage();
        }

        private void RenderRivalSectorAgentsPage()
        {
            if (selectedRival == null || selectedSector == null)
            {
                currentPage = PageState.RivalList;
                RefreshPage();
                return;
            }

            pageTitleText.text = selectedRival.Definition.DisplayName + " - " + selectedSector.DisplayName;

            var backButton = CreateButton(contentRoot, "BackButton", "← Geri");
            var backRect = backButton.GetComponent<RectTransform>();
            backRect.sizeDelta = new Vector2(0f, 40f);
            backButton.onClick.AddListener(() =>
            {
                currentPage = PageState.RivalList;
                selectedRival = null;
                selectedSector = null;
                RefreshPage();
            });

            var activeJobs = selectedRival.ActiveJobs;
            var sectorJobCount = 0;
            var agentAffectedCount = 0;
            for (var i = 0; i < activeJobs.Count; i++)
            {
                if (activeJobs[i].Sector != selectedSector) continue;
                sectorJobCount++;
                if (activeJobs[i].IsAgentAffected) agentAffectedCount++;
            }

            SharedBuilder.Clear();
            SharedBuilder.Append("Bu Sektördeki İşler: ");
            SharedBuilder.Append(sectorJobCount);
            if (agentAffectedCount > 0)
            {
                SharedBuilder.Append("\nAjan Etkisi Altında: ");
                SharedBuilder.Append(agentAffectedCount);
                SharedBuilder.Append(" (rekabete dahil edilmiyor)");
            }

            var activeAgentCount = agentManager != null ? agentManager.GetActiveAgentCountForRivalSector(selectedRival, selectedSector) : 0;
            if (activeAgentCount > 0)
            {
                SharedBuilder.Append("\nAktif Ajanlarınız: ");
                SharedBuilder.Append(activeAgentCount);
            }

            CreateInfoCard(SharedBuilder.ToString(), 80f);

            if (agentManager != null)
            {
                RenderFailedAgentsForSector();
                RenderActiveAgentsForSector();
            }

            SharedBuilder.Clear();
            SharedBuilder.Append("Gönderilebilir Ajanlar");
            if (agentManager != null)
            {
                SharedBuilder.Append(" (Yenilenmeye ");
                SharedBuilder.Append(agentManager.DaysUntilNextRefresh);
                SharedBuilder.Append(" gün)");
            }

            CreateInfoCard(SharedBuilder.ToString(), 36f);

            if (agentManager == null)
            {
                CreateInfoCard("Ajan sistemi bulunamadı.", 36f);
                return;
            }

            var agents = agentManager.GetAvailableAgents();
            if (agents.Count == 0)
            {
                CreateInfoCard("Havuzda ajan kalmadı. Yenilenmeyi bekleyin.", 36f);
                return;
            }

            for (var i = 0; i < agents.Count; i++)
            {
                RenderAgentCard(agents[i]);
            }
        }

        private void RenderFailedAgentsForSector()
        {
            var failed = agentManager.FailedAgents;
            for (var i = 0; i < failed.Count; i++)
            {
                var agent = failed[i];
                if (agent.TargetRival != selectedRival || agent.TargetSector != selectedSector)
                {
                    continue;
                }

                SharedBuilder.Clear();
                SharedBuilder.Append(agent.Definition.DisplayName);
                SharedBuilder.Append(" - Başarısız oldu! Hiçbir işi etkileyemedi. Maliyet: ");
                SharedBuilder.Append(agent.Cost.Amount.ToString("N0"));

                var card = CreateInfoCard(SharedBuilder.ToString(), 44f);
                card.color = new Color(1f, 0.55f, 0.55f, 1f);
            }
        }

        private void RenderActiveAgentsForSector()
        {
            var deployed = agentManager.DeployedAgents;
            var hasActive = false;

            for (var i = 0; i < deployed.Count; i++)
            {
                var agent = deployed[i];
                if (!agent.IsActive || agent.TargetRival != selectedRival || agent.TargetSector != selectedSector)
                {
                    continue;
                }

                if (!hasActive)
                {
                    CreateInfoCard("Aktif Ajanlar", 36f);
                    hasActive = true;
                }

                SharedBuilder.Clear();
                SharedBuilder.Append(agent.Definition.DisplayName);
                SharedBuilder.Append(" | Kalan Gün: ");
                SharedBuilder.Append(agent.RemainingDays);
                SharedBuilder.Append(" | Etkilenen İş: ");
                SharedBuilder.Append(agent.AffectedJobs.Count);
                SharedBuilder.Append(" | Maliyet: ");
                SharedBuilder.Append(agent.Cost.Amount.ToString("N0"));

                CreateInfoCard(SharedBuilder.ToString(), 44f);
            }
        }

        private void RenderAgentCard(AgentDefinition agentDef)
        {
            SharedBuilder.Clear();
            SharedBuilder.Append(agentDef.DisplayName);
            SharedBuilder.Append("\nSüre: ");
            SharedBuilder.Append(agentDef.DetectionDurationDays);
            SharedBuilder.Append(" gün | Maks Sabotaj: ");
            SharedBuilder.Append(agentDef.MaxSimultaneousSabotage);
            SharedBuilder.Append(" iş | Başarı: %");
            SharedBuilder.Append((agentDef.SuccessChance * 100f).ToString("F0"));
            SharedBuilder.Append("\nGelir Düşürme: %");
            SharedBuilder.Append(((1f - agentDef.RevenueReductionMultiplier) * 100f).ToString("F0"));
            SharedBuilder.Append(" | Maliyet: ");
            SharedBuilder.Append(agentDef.MinimumCost.ToString("N0"));
            SharedBuilder.Append(" - ");
            SharedBuilder.Append(agentDef.MaximumCost.ToString("N0"));

            CreateInfoCard(SharedBuilder.ToString(), 80f);

            var canDeploy = agentManager.CanDeployAgent(agentDef);
            var capturedDef = agentDef;
            var deployButton = CreateButton(contentRoot, "DeployBtn_" + agentDef.Id, canDeploy ? "Ajan Gönder" : "Yetersiz Bakiye");
            var deployRect = deployButton.GetComponent<RectTransform>();
            deployRect.sizeDelta = new Vector2(0f, 40f);

            if (canDeploy)
            {
                deployButton.onClick.AddListener(() =>
                {
                    agentManager.DeployAgent(capturedDef, selectedRival, selectedSector);
                });
            }
            else
            {
                deployButton.interactable = false;
            }
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
            var button = CreateButton(rootCanvas.transform, "RivalCompanyOpenButton", "Rakip Şirketler");
            var buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 1f);
            buttonRect.anchorMax = new Vector2(0f, 1f);
            buttonRect.pivot = new Vector2(0f, 1f);
            buttonRect.anchoredPosition = new Vector2(1050f, -80f);
            buttonRect.sizeDelta = new Vector2(220f, 44f);
            button.onClick.AddListener(OpenPanel);
        }

        private void CreatePanel()
        {
            panelRoot = CreateUiObject("RivalCompanyPanel", rootCanvas.transform);
            var panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(0f, -10f);
            panelRect.sizeDelta = panelSize;

            panelRoot.AddComponent<Image>().color = new Color(0.1f, 0.12f, 0.16f, 0.98f);

            var headerRoot = CreateUiObject("Header", panelRoot.transform);
            var headerRect = headerRoot.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0f, 70f);
            headerRoot.AddComponent<Image>().color = new Color(0.17f, 0.21f, 0.29f, 1f);

            pageTitleText = CreateText(headerRoot.transform, "Rakip Şirketler", 28, TextAnchor.MiddleLeft);
            RuntimePanelUiUtility.StretchToParent(pageTitleText.rectTransform, 18f, 8f, 140f, 8f);

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
