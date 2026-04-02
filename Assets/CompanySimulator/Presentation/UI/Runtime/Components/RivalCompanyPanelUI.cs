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
        [SerializeField] private SecurityPanelUI securityPanelUI;
        [SerializeField] private ShopPanelUI shopPanelUI;
        [SerializeField] private InventoryPanelUI inventoryPanelUI;
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private Vector2 panelSize = new Vector2(980f, 720f);
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
        private Button backButton;
        private Transform deployableAgentGridParent;

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
            securityPanelUI ??= FindObjectOfType<SecurityPanelUI>();
            shopPanelUI ??= FindObjectOfType<ShopPanelUI>();
            inventoryPanelUI ??= FindObjectOfType<InventoryPanelUI>();
            EnsureCanvas();
            EnsureEventSystem();
            defaultFont = LoadDefaultFont();
            roundedSprite = LoadRoundedSprite();
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
            if (securityPanelUI != null && securityPanelUI.IsOpen) securityPanelUI.ClosePanel();
            if (shopPanelUI != null && shopPanelUI.IsOpen) shopPanelUI.ClosePanel();
            if (inventoryPanelUI != null && inventoryPanelUI.IsOpen) inventoryPanelUI.ClosePanel();

            currentPage = PageState.RivalList;
            selectedRival = null;
            selectedSector = null;
            panelRoot.SetActive(true);
            RuntimePanelUiUtility.BringToFront(panelRoot);
            RefreshPage();
        }

        public void ClosePanel()
        {
            panelRoot.SetActive(false);
        }

        private void GoBack()
        {
            if (currentPage == PageState.RivalSectorAgents)
            {
                currentPage = PageState.RivalList;
                selectedRival = null;
                selectedSector = null;
                RefreshPage();
            }
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
            deployableAgentGridParent = null;
            UpdateHeaderButtons();

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
            var accent = GetRivalAccent(rival);
            const float sectorCardWidth = 400f;
            const float sectorCardHeight = 92f;
            const float sectorGridSpacing = 36f;
            var sectorColumnCount = Mathf.Max(1, CalculateGridColumnCount(sectorCardWidth, sectorGridSpacing));
            var sectorRowCount = sectors.Count > 0 ? Mathf.CeilToInt(sectors.Count / (float)sectorColumnCount) : 1;
            var sectorGridHeight = (sectorRowCount * sectorCardHeight) + (Mathf.Max(0, sectorRowCount - 1) * sectorGridSpacing);
            var cardHeight = 148f + sectorGridHeight;
            var card = CreateSurface(contentRoot, "Rival_" + rival.Definition.Id, cardHeight, ColPanel);
            AddHoverEffect(card, ColPanel, Blend(ColPanel, accent, 0.18f));
            CreateAccentBar(card.transform, accent);

            var content = CreateStretchContainer(card.transform, "Content", 12f, 12f, 12f, 12f);
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 0, 0, 0);
            contentLayout.spacing = 8f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var topRow = CreateUiObject("TopRow", content.transform);
            topRow.AddComponent<LayoutElement>().preferredHeight = 72f;
            var topLayout = topRow.AddComponent<HorizontalLayoutGroup>();
            topLayout.spacing = 12f;
            topLayout.childControlWidth = true;
            topLayout.childControlHeight = true;
            topLayout.childForceExpandWidth = false;
            topLayout.childForceExpandHeight = false;
            topLayout.childAlignment = TextAnchor.MiddleLeft;

            var title = CreateText(topRow.transform, rival.Definition.DisplayName, 33, TextAnchor.MiddleLeft);
            title.color = ColText;
            title.fontStyle = FontStyle.Bold;
            var titleLayout = title.gameObject.AddComponent<LayoutElement>();
            titleLayout.flexibleWidth = 1f;
            titleLayout.preferredHeight = 72f;

            var statRow = CreateUiObject("StatsRow", topRow.transform);
            var statRowLayout = statRow.AddComponent<LayoutElement>();
            statRowLayout.preferredWidth = 546f;
            statRowLayout.minWidth = 546f;
            statRowLayout.preferredHeight = 72f;
            var statGrid = statRow.AddComponent<GridLayoutGroup>();
            statGrid.cellSize = new Vector2(174f, 72f);
            statGrid.spacing = new Vector2(12f, 0f);
            statGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            statGrid.constraintCount = 3;
            statGrid.childAlignment = TextAnchor.MiddleCenter;

            CreateRivalHeaderStat(statRow.transform, rival.Balance.Amount.ToString("N0"), "Bakiye");
            CreateRivalHeaderStat(statRow.transform, rival.CompanyValue.Amount.ToString("N0"), "Değer");
            CreateRivalHeaderStat(statRow.transform, rival.ActiveJobCount.ToString(), "Aktif İş");

            var sectorGridHost = CreateUiObject("SectorGrid", content.transform);
            var sectorGridLayout = sectorGridHost.AddComponent<LayoutElement>();
            sectorGridLayout.preferredHeight = sectorGridHeight;
            sectorGridLayout.minHeight = sectorGridHeight;
            var sectorGrid = sectorGridHost.AddComponent<GridLayoutGroup>();
            sectorGrid.cellSize = new Vector2(sectorCardWidth, sectorCardHeight);
            sectorGrid.spacing = new Vector2(sectorGridSpacing, sectorGridSpacing);
            sectorGrid.padding = new RectOffset(0, 0, 0, 0);
            sectorGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            sectorGrid.constraintCount = sectorColumnCount;
            sectorGrid.childAlignment = TextAnchor.UpperCenter;

            for (var i = 0; i < sectors.Count; i++)
            {
                var sector = sectors[i];
                sectorJobCounts.TryGetValue(sector, out var jobCount);
                CreateRivalSectorCard(sectorGridHost.transform, rival, sector, jobCount, accent);
            }

            if (sectors.Count == 0)
            {
                var emptyState = CreateText(content.transform, "Henüz aktif sektör yok.", 13, TextAnchor.MiddleLeft);
                emptyState.color = ColMuted;
                emptyState.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
            }
        }

        private void CreateRivalSectorCard(Transform parent, RivalCompanyRuntimeData rival, SectorDefinition sector, int jobCount, Color accent)
        {
            var card = CreateSurface(parent, "SectorCard_" + rival.Definition.Id + "_" + sector.Id, 92f, ColSurfaceAlt);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(400f, 92f);
            var cardLayout = card.GetComponent<LayoutElement>();
            cardLayout.preferredWidth = 400f;
            cardLayout.minWidth = 400f;
            AddHoverEffect(card, ColSurfaceAlt, Blend(ColSurfaceAlt, accent, 0.18f));
            CreateAccentBar(card.transform, accent);

            var button = card.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.colors = CreateButtonColors(ColSurfaceAlt, Blend(ColSurfaceAlt, accent, 0.18f), Darken(ColSurfaceAlt, 0.12f));
            button.onClick.AddListener(() => NavigateToSectorAgents(rival, sector));

            var content = CreateStretchContainer(card.transform, "Content", 12f, 12f, 12f, 12f);
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 0, 0, 0);
            contentLayout.spacing = 4f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var title = CreateText(content.transform, sector.DisplayName, 18, TextAnchor.MiddleLeft);
            title.color = ColText;
            title.fontStyle = FontStyle.Bold;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

            var subtitle = CreateText(content.transform, "Aktif İş: " + jobCount, 13, TextAnchor.MiddleLeft);
            subtitle.color = ColMuted;
            subtitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
        }

        private void CreateRivalHeaderStat(Transform parent, string value, string label)
        {
            var tile = CreateSurface(parent, "RivalHeaderStat", 72f, new Color(ColSurfaceAlt.r, ColSurfaceAlt.g, ColSurfaceAlt.b, 0.95f));
            var content = CreateStretchContainer(tile.transform, "Content", 8f, 8f, 8f, 8f);
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 0, 0, 0);
            contentLayout.spacing = 2f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childAlignment = TextAnchor.MiddleCenter;

            CreateFlexibleSpacer(content.transform);

            var valueText = CreateText(content.transform, value, 22, TextAnchor.MiddleCenter);
            valueText.color = ColText;
            valueText.fontStyle = FontStyle.Bold;
            valueText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

            var labelText = CreateText(content.transform, label, 12, TextAnchor.MiddleCenter);
            labelText.color = ColMuted;
            labelText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

            CreateFlexibleSpacer(content.transform);
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

            CreateInfoCard(SharedBuilder.ToString(), 92f);

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

            CreateSectionTitle(SharedBuilder.ToString());

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

                var card = CreateInfoCard(SharedBuilder.ToString(), 52f);
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
                    CreateSectionTitle("Aktif Ajanlar");
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

                CreateInfoCard(SharedBuilder.ToString(), 52f);
            }
        }

        private void RenderAgentCard(AgentDefinition agentDef)
        {
            if (deployableAgentGridParent == null)
            {
                deployableAgentGridParent = CreateGridHost("DeployableAgentGrid", 400f, 214f).transform;
            }

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

            var canDeploy = agentManager.CanDeployAgent(agentDef);
            var capturedDef = agentDef;
            var accent = canDeploy ? ColPurple : ColSurfaceAlt;
            var card = CreateSurface(deployableAgentGridParent, "Agent_" + agentDef.Id, 214f, ColPanel);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(400f, 214f);
            var cardLayout = card.GetComponent<LayoutElement>();
            cardLayout.preferredWidth = 400f;
            cardLayout.minWidth = 400f;
            AddHoverEffect(card, ColPanel, Blend(ColPanel, ColPurple, 0.18f));
            CreateAccentBar(card.transform, ColPurple);

            var content = CreateStretchContainer(card.transform, "Content", 12f, 12f, 12f, 12f);
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 0, 0, 0);
            contentLayout.spacing = 8f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var title = CreateText(content.transform, agentDef.DisplayName, 18, TextAnchor.MiddleLeft);
            title.color = ColText;
            title.fontStyle = FontStyle.Bold;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

            var detail = CreateText(content.transform, SharedBuilder.ToString().Replace(agentDef.DisplayName + "\n", string.Empty), 13, TextAnchor.MiddleLeft);
            detail.color = ColMuted;
            detail.gameObject.AddComponent<LayoutElement>().preferredHeight = 74f;

            CreateFlexibleSpacer(content.transform);

            var deployButton = CreateStyledButton(content.transform, "DeployBtn_" + agentDef.Id, canDeploy ? "Ajan Gönder" : "Yetersiz Bakiye", canDeploy ? ColPurple : ColSurfaceAlt, canDeploy ? Blend(ColPurple, ColCyan, 0.18f) : Blend(ColSurfaceAlt, ColBlue, 0.1f), canDeploy ? Darken(ColPurple, 0.16f) : Darken(ColSurfaceAlt, 0.08f), canDeploy ? ColText : ColMuted, TextAnchor.MiddleCenter);
            deployButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;

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
            var button = CreateStyledButton(rootCanvas.transform, "RivalCompanyOpenButton", "Rakip Şirketler", ColSurface, Blend(ColSurface, ColBlue, 0.25f), Darken(ColSurface, 0.16f), ColText, TextAnchor.MiddleCenter);
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
            var badgeText = CreateText(badge.transform, "RIV", 16, TextAnchor.MiddleCenter);
            badgeText.color = ColCyan;
            badgeText.fontStyle = FontStyle.Bold;
            RuntimePanelUiUtility.StretchToParent(badgeText.rectTransform, 0f, 0f, 0f, 0f);

            pageTitleText = CreateText(headerRoot.transform, "Rakip Şirketler", 28, TextAnchor.MiddleLeft);
            pageTitleText.color = ColText;
            pageTitleText.fontStyle = FontStyle.Bold;
            pageTitleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            pageTitleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            pageTitleText.rectTransform.offsetMin = new Vector2(86f, -50f);
            pageTitleText.rectTransform.offsetMax = new Vector2(-140f, -14f);

            backButton = CreateStyledButton(headerRoot.transform, "BackButton", "←", ColSurfaceAlt, Blend(ColSurfaceAlt, ColBlue, 0.18f), Darken(ColSurfaceAlt, 0.15f), ColText, TextAnchor.MiddleCenter);
            var backRect = backButton.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(1f, 0.5f);
            backRect.anchorMax = new Vector2(1f, 0.5f);
            backRect.pivot = new Vector2(1f, 0.5f);
            backRect.anchoredPosition = new Vector2(-72f, 0f);
            backRect.sizeDelta = new Vector2(50f, 40f);
            backButton.onClick.AddListener(GoBack);

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
            layout.spacing = 36f;
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

        private Text CreateInfoCard(string message, float height = 58f)
        {
            var card = CreateSurface(contentRoot, "InfoCard", height, ColSurface);
            var text = CreateText(card.transform, message, 18, TextAnchor.MiddleLeft);
            text.color = ColMuted;
            RuntimePanelUiUtility.StretchToParent(text.rectTransform, 14f, 8f, 14f, 8f);
            return text;
        }

        private void CreateSectionTitle(string title)
        {
            var titleText = CreateText(contentRoot, title, 20, TextAnchor.MiddleLeft);
            titleText.rectTransform.sizeDelta = new Vector2(0f, 34f);
            titleText.color = ColText;
            titleText.fontStyle = FontStyle.Bold;
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
            RuntimePanelUiUtility.StretchToParent(text.rectTransform, 16f, 8f, 16f, 8f);
            return button;
        }

        private GameObject CreateGridHost(string objectName, float cardWidth, float cardHeight)
        {
            const float gridSpacing = 36f;
            var host = CreateUiObject(objectName, contentRoot);
            var grid = host.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(cardWidth, cardHeight);
            grid.spacing = new Vector2(gridSpacing, gridSpacing);
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = CalculateGridColumnCount(cardWidth, gridSpacing);
            grid.childAlignment = TextAnchor.UpperLeft;

            var fitter = host.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return host;
        }

        private int CalculateGridColumnCount(float cardWidth, float spacing)
        {
            const float horizontalPadding = 80f;
            var availableWidth = Mathf.Max(cardWidth, panelSize.x - horizontalPadding);
            return Mathf.Max(1, Mathf.FloorToInt((availableWidth + spacing) / (cardWidth + spacing)));
        }

        private Color GetRivalAccent(RivalCompanyRuntimeData rival)
        {
            var key = rival != null && rival.Definition != null ? rival.Definition.Id ?? rival.Definition.DisplayName ?? string.Empty : string.Empty;
            switch (Mathf.Abs(key.GetHashCode()) % 5)
            {
                case 0:
                    return ColBlue;
                case 1:
                    return ColGold;
                case 2:
                    return ColPurple;
                case 3:
                    return ColCyan;
                default:
                    return ColGreen;
            }
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
            RuntimePanelUiUtility.StretchToParent(container.GetComponent<RectTransform>(), left, bottom, right, top);
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

            var tagFitter = tag.AddComponent<ContentSizeFitter>();
            tagFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            tagFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var text = CreateText(tag.transform, value, fontSize, TextAnchor.MiddleCenter);
            text.color = textColor;
            text.fontStyle = FontStyle.Bold;
            var textLayout = text.gameObject.AddComponent<LayoutElement>();
            textLayout.preferredHeight = fontSize >= 14 ? 18f : 16f;
            return tag;
        }

        private GameObject CreateMiniStat(Transform parent, string value, string label)
        {
            var tile = CreateSurface(parent, "MiniStat", 42f, new Color(ColSurfaceAlt.r, ColSurfaceAlt.g, ColSurfaceAlt.b, 0.95f));
            var valueText = CreateText(tile.transform, value, 17, TextAnchor.UpperCenter);
            valueText.color = ColText;
            valueText.fontStyle = FontStyle.Bold;
            RuntimePanelUiUtility.StretchToParent(valueText.rectTransform, 6f, 18f, 6f, 3f);

            var labelText = CreateText(tile.transform, label, 11, TextAnchor.LowerCenter);
            labelText.color = ColMuted;
            RuntimePanelUiUtility.StretchToParent(labelText.rectTransform, 6f, 3f, 6f, 21f);
            return tile;
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

        private void UpdateHeaderButtons()
        {
            if (backButton != null)
            {
                backButton.gameObject.SetActive(currentPage != PageState.RivalList);
            }
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
