using CompanySimulator.Presentation.UI.Runtime.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CompanySimulator.Presentation.UI.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class DesignSandboxPanelUI : MonoBehaviour
    {
        [SerializeField] private SectorPanelUI sectorPanelUI;
        [SerializeField] private EmployeePanelUI employeePanelUI;
        [SerializeField] private AccountingPanelUI accountingPanelUI;
        [SerializeField] private BankPanelUI bankPanelUI;
        [SerializeField] private FinanceOverviewPanelUI financeOverviewPanelUI;
        [SerializeField] private RivalCompanyPanelUI rivalCompanyPanelUI;
        [SerializeField] private DebugPanelUI debugPanelUI;
        [SerializeField] private SecurityPanelUI securityPanelUI;
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private Sprite appIcon;
        [SerializeField] private Vector2 panelSize = new Vector2(980f, 760f);
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
        private Text pageSubtitleText;
        private bool showEmployeeCandidates;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            sectorPanelUI ??= FindObjectOfType<SectorPanelUI>();
            employeePanelUI ??= FindObjectOfType<EmployeePanelUI>();
            accountingPanelUI ??= FindObjectOfType<AccountingPanelUI>();
            bankPanelUI ??= FindObjectOfType<BankPanelUI>();
            financeOverviewPanelUI ??= FindObjectOfType<FinanceOverviewPanelUI>();
            rivalCompanyPanelUI ??= FindObjectOfType<RivalCompanyPanelUI>();
            debugPanelUI ??= FindObjectOfType<DebugPanelUI>();
            securityPanelUI ??= FindObjectOfType<SecurityPanelUI>();

            EnsureCanvas();
            EnsureEventSystem();
            defaultFont = LoadDefaultFont();
            roundedSprite = LoadRoundedSprite();
            BuildUi();
        }

        public void OpenPanel()
        {
            RuntimePanelUiUtility.SetComputerPanelActive(rootCanvas, true);

            if (sectorPanelUI != null && sectorPanelUI.IsOpen) sectorPanelUI.ClosePanel();
            if (employeePanelUI != null && employeePanelUI.IsOpen) employeePanelUI.ClosePanel();
            if (accountingPanelUI != null && accountingPanelUI.IsOpen) accountingPanelUI.ClosePanel();
            if (bankPanelUI != null && bankPanelUI.IsOpen) bankPanelUI.ClosePanel();
            if (financeOverviewPanelUI != null && financeOverviewPanelUI.IsOpen) financeOverviewPanelUI.ClosePanel();
            if (rivalCompanyPanelUI != null && rivalCompanyPanelUI.IsOpen) rivalCompanyPanelUI.ClosePanel();
            if (debugPanelUI != null && debugPanelUI.IsOpen) debugPanelUI.ClosePanel();
            if (securityPanelUI != null && securityPanelUI.IsOpen) securityPanelUI.ClosePanel();

            panelRoot.SetActive(true);
            RuntimePanelUiUtility.BringToFront(panelRoot);
            RefreshPage();
        }

        public void ClosePanel()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void BuildUi()
        {
            CreateOpenButton();
            CreatePanel();
            panelRoot.SetActive(false);
        }

        private void RefreshPage()
        {
            if (contentRoot == null)
            {
                return;
            }

            ClearChildren(contentRoot);
            pageTitleText.text = "UI Lab";
            pageSubtitleText.text = "Futuristic rounded design sandbox";

            RenderHeroCard();
            RenderPaletteStrip();
            RenderSectorPreviewGrid();
            RenderProjectPreviewCard();
            RenderEmployeeSlotShowcase();
            RenderControlsShowcase();
        }

        private void RenderHeroCard()
        {
            var hero = CreateSurface(contentRoot, "HeroCard", 156f, ColPanel);
            CreateAccentBar(hero.transform, ColCyan);

            var icon = CreateRoundedBlock(hero.transform, "HeroIcon", new Vector2(56f, 56f), new Color(ColBlue.r, ColBlue.g, ColBlue.b, 0.2f));
            var iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 1f);
            iconRect.anchorMax = new Vector2(0f, 1f);
            iconRect.pivot = new Vector2(0f, 1f);
            iconRect.anchoredPosition = new Vector2(18f, -16f);
            var iconText = CreateText(icon.transform, "UI", 20, TextAnchor.MiddleCenter);
            iconText.color = ColBlue;
            iconText.fontStyle = FontStyle.Bold;
            StretchToParent(iconText.rectTransform, 0f, 0f, 0f, 0f);

            var title = CreateText(hero.transform, "Tasarım Test Paneli", 28, TextAnchor.UpperLeft);
            title.color = ColText;
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -18f);
            title.rectTransform.sizeDelta = new Vector2(-110f, 34f);
            title.rectTransform.offsetMin = new Vector2(92f, 0f);
            title.rectTransform.offsetMax = new Vector2(-20f, -18f);

            var subtitle = CreateText(hero.transform, "Önce burada stil oturt, sonra aynı helper'ları sektör paneline taşı.", 16, TextAnchor.UpperLeft);
            subtitle.color = ColMuted;
            subtitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            subtitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            subtitle.rectTransform.pivot = new Vector2(0.5f, 1f);
            subtitle.rectTransform.anchoredPosition = new Vector2(0f, -54f);
            subtitle.rectTransform.sizeDelta = new Vector2(-110f, 24f);
            subtitle.rectTransform.offsetMin = new Vector2(92f, 0f);
            subtitle.rectTransform.offsetMax = new Vector2(-20f, -54f);

            var tagRow = CreateHorizontalGroup(hero.transform, "HeroTags", new RectOffset(0, 0, 0, 0), 8f);
            var tagRowRect = tagRow.GetComponent<RectTransform>();
            tagRowRect.anchorMin = new Vector2(0f, 0f);
            tagRowRect.anchorMax = new Vector2(1f, 0f);
            tagRowRect.pivot = new Vector2(0.5f, 0f);
            tagRowRect.anchoredPosition = new Vector2(0f, 18f);
            tagRowRect.sizeDelta = new Vector2(-32f, 30f);

            CreateTag(tagRow.transform, "Rounded", new Color(ColCyan.r, ColCyan.g, ColCyan.b, 0.18f), ColCyan);
            CreateTag(tagRow.transform, "Glow Hover", new Color(ColBlue.r, ColBlue.g, ColBlue.b, 0.18f), ColBlue);
            CreateTag(tagRow.transform, "Dark Futuristic", new Color(ColPurple.r, ColPurple.g, ColPurple.b, 0.18f), ColPurple);
        }

        private void RenderPaletteStrip()
        {
            CreateSectionTitle("Tasarım Tokenları");

            var row = CreateUiObject("PaletteRow", contentRoot);
            row.AddComponent<LayoutElement>().preferredHeight = 88f;
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            CreateMetricCard(row.transform, "Ana Renk", "Blue", ColBlue, "CTA");
            CreateMetricCard(row.transform, "Vurgu", "Cyan", ColCyan, "Hover");
            CreateMetricCard(row.transform, "Risk", "Gold", ColGold, "Mid");
            CreateMetricCard(row.transform, "Başarı", "Green", ColGreen, "Low");
            CreateMetricCard(row.transform, "Tehlike", "Red", ColRed, "High");
        }

        private void RenderSectorPreviewGrid()
        {
            CreateSectionTitle("Örnek Sektör Kartları");

            const int sectorCardCount = 6;
            const float sectorCardWidth = 400f;
            const float sectorCardHeight = 186f;
            const float sectorGridSpacing = 36f;
            var sectorColumnCount = CalculateGridColumnCount(sectorCardWidth, sectorGridSpacing);
            var sectorGridHeight = CalculateGridHeight(sectorCardCount, sectorColumnCount, sectorCardHeight, sectorGridSpacing);

            var gridHost = CreateUiObject("SectorGrid", contentRoot);
            var gridHostLayout = gridHost.AddComponent<LayoutElement>();
            gridHostLayout.preferredHeight = sectorGridHeight;
            gridHostLayout.minHeight = sectorGridHeight;

            var grid = gridHost.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(sectorCardWidth, sectorCardHeight);
            grid.spacing = new Vector2(sectorGridSpacing, sectorGridSpacing);
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = sectorColumnCount;
            grid.childAlignment = TextAnchor.UpperCenter;

            CreateSectorPreviewCard(gridHost.transform, "Advertising", 2, 7, "Düşük Risk", ColGreen);
            CreateSectorPreviewCard(gridHost.transform, "Application Development", 4, 14, "Orta Risk", ColGold);
            CreateSectorPreviewCard(gridHost.transform, "Film Series Production", 1, 21, "Yüksek Risk", ColRed);
            CreateSectorPreviewCard(gridHost.transform, "Game Development", 3, 10, "Orta Risk", ColPurple);
            CreateSectorPreviewCard(gridHost.transform, "AI Automation", 5, 9, "Orta Risk", ColCyan);
            CreateSectorPreviewCard(gridHost.transform, "SaaS Platform", 6, 12, "Düşük Risk", ColBlue);
        }

        private void RenderProjectPreviewCard()
        {
            CreateSectionTitle("Aktif İş Kartı Örnekleri");

            const int projectCardCount = 6;
            const float projectCardWidth = 400f;
            const float projectCardHeight = 228f;
            const float projectGridSpacing = 36f;
            var projectColumnCount = CalculateGridColumnCount(projectCardWidth, projectGridSpacing);
            var projectGridHeight = CalculateGridHeight(projectCardCount, projectColumnCount, projectCardHeight, projectGridSpacing);

            var host = CreateUiObject("ProjectPreviewHost", contentRoot);
            var hostLayout = host.AddComponent<LayoutElement>();
            hostLayout.preferredHeight = projectGridHeight;
            hostLayout.minHeight = projectGridHeight;

            var grid = host.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(projectCardWidth, projectCardHeight);
            grid.spacing = new Vector2(projectGridSpacing, projectGridSpacing);
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = projectColumnCount;
            grid.childAlignment = TextAnchor.UpperCenter;

            CreateProjectPreviewSampleCard(host.transform, "Neon Commerce App", "5g", "96,000", "31,500", "Ece, Mert, Sarp", ColBlue);
            CreateProjectPreviewSampleCard(host.transform, "Quantum Ads Campaign", "7g", "124,000", "40,200", "Deniz, Arda", ColCyan);
            CreateProjectPreviewSampleCard(host.transform, "Cinematic Trailer Pack", "12g", "188,000", "72,600", "Selin, Kerem, Bora", ColRed);
            CreateProjectPreviewSampleCard(host.transform, "Mobile Puzzle LiveOps", "6g", "84,000", "26,900", "Eylül, Tuna", ColPurple);
            CreateProjectPreviewSampleCard(host.transform, "AI Support Suite", "9g", "142,000", "55,400", "Mina, Burak, Kaan", ColGreen);
            CreateProjectPreviewSampleCard(host.transform, "Cloud CRM Launch", "8g", "118,000", "44,800", "Ela, Cem", ColGold);
        }

        private void RenderEmployeeSlotShowcase()
        {
            CreateSectionTitle("Örnek Çalışan Slotu");

            const float slotCardWidth = 400f;
            const float slotCardHeight = 96f;
            const float candidateCardWidth = 400f;
            const float candidateCardHeight = 128f;
            const float gridSpacing = 36f;
            const int candidateCount = 6;

            var columnCount = CalculateGridColumnCount(candidateCardWidth, gridSpacing);
            var candidateGridHeight = CalculateGridHeight(candidateCount, columnCount, candidateCardHeight, gridSpacing);
            var sectionHeight = slotCardHeight + 12f + (showEmployeeCandidates ? candidateGridHeight : 0f);

            var host = CreateUiObject("EmployeeSlotHost", contentRoot);
            var hostLayout = host.AddComponent<LayoutElement>();
            hostLayout.preferredHeight = sectionHeight;
            hostLayout.minHeight = sectionHeight;

            var hostVertical = host.AddComponent<VerticalLayoutGroup>();
            hostVertical.padding = new RectOffset(0, 0, 0, 0);
            hostVertical.spacing = 12f;
            hostVertical.childControlWidth = true;
            hostVertical.childControlHeight = true;
            hostVertical.childForceExpandWidth = true;
            hostVertical.childForceExpandHeight = false;

            var slotRow = CreateUiObject("EmployeeSlotRow", host.transform);
            slotRow.AddComponent<LayoutElement>().preferredHeight = slotCardHeight;
            var slotRowLayout = slotRow.AddComponent<HorizontalLayoutGroup>();
            slotRowLayout.childAlignment = TextAnchor.UpperCenter;
            slotRowLayout.childControlWidth = true;
            slotRowLayout.childControlHeight = true;
            slotRowLayout.childForceExpandWidth = false;
            slotRowLayout.childForceExpandHeight = false;

            CreateEmployeeSlotCard(slotRow.transform, slotCardWidth, slotCardHeight, showEmployeeCandidates);

            if (!showEmployeeCandidates)
            {
                return;
            }

            var candidateGridHost = CreateUiObject("EmployeeCandidateGrid", host.transform);
            var candidateGridLayout = candidateGridHost.AddComponent<LayoutElement>();
            candidateGridLayout.preferredHeight = candidateGridHeight;
            candidateGridLayout.minHeight = candidateGridHeight;

            var grid = candidateGridHost.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(candidateCardWidth, candidateCardHeight);
            grid.spacing = new Vector2(gridSpacing, gridSpacing);
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columnCount;
            grid.childAlignment = TextAnchor.UpperCenter;

            CreateEmployeeCandidateSampleCard(candidateGridHost.transform, "Ece Yılmaz", "Backend Developer", "Senior", "85,000", ColBlue);
            CreateEmployeeCandidateSampleCard(candidateGridHost.transform, "Mert Kaya", "Game Designer", "Mid", "62,000", ColPurple);
            CreateEmployeeCandidateSampleCard(candidateGridHost.transform, "Selin Aras", "UI Artist", "Senior", "78,000", ColCyan);
            CreateEmployeeCandidateSampleCard(candidateGridHost.transform, "Arda Demir", "Marketing Lead", "Lead", "91,000", ColGold);
            CreateEmployeeCandidateSampleCard(candidateGridHost.transform, "Mina Çetin", "AI Engineer", "Senior", "96,000", ColGreen);
            CreateEmployeeCandidateSampleCard(candidateGridHost.transform, "Bora Şahin", "Producer", "Mid", "58,000", ColRed);
        }

        private void RenderControlsShowcase()
        {
            CreateSectionTitle("Buton ve Input Testi");

            const float bottomButtonSpacing = 10f;
            var bottomButtonWidth = Mathf.Min(400f, Mathf.Max(220f, ((panelSize.x - 120f) - bottomButtonSpacing) * 0.5f));

            var card = CreateSurface(contentRoot, "ControlsCard", 238f, ColSurface);

            var content = CreateStretchContainer(card.transform, "Content", 18f, 18f, 18f, 14f);
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 0, 0, 0);
            contentLayout.spacing = 8f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var intro = CreateText(content.transform, "Aynı panel içinde primary / secondary button, pill tag ve input görünüşünü test et.", 16, TextAnchor.MiddleLeft);
            intro.color = ColMuted;
            intro.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

            var tagRow = CreateHorizontalGroup(content.transform, "ControlTags", new RectOffset(0, 0, 0, 0), 8f);
            tagRow.AddComponent<LayoutElement>().preferredHeight = 30f;

            CreateTag(tagRow.transform, "Live Preview", new Color(ColGreen.r, ColGreen.g, ColGreen.b, 0.18f), ColGreen);
            CreateTag(tagRow.transform, "Rounded Input", new Color(ColBlue.r, ColBlue.g, ColBlue.b, 0.18f), ColBlue);
            CreateTag(tagRow.transform, "No Sharp Corners", new Color(ColPurple.r, ColPurple.g, ColPurple.b, 0.18f), ColPurple);

            var inputLabel = CreateText(content.transform, "Bütçe Simülasyonu", 16, TextAnchor.MiddleLeft);
            inputLabel.color = ColText;
            inputLabel.fontStyle = FontStyle.Bold;
            inputLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

            var inputRow = CreateUiObject("InputRow", content.transform);
            inputRow.AddComponent<LayoutElement>().preferredHeight = 40f;
            var inputRowLayout = inputRow.AddComponent<HorizontalLayoutGroup>();
            inputRowLayout.spacing = 0f;
            inputRowLayout.padding = new RectOffset(0, 0, 0, 0);
            inputRowLayout.childControlWidth = true;
            inputRowLayout.childControlHeight = true;
            inputRowLayout.childForceExpandWidth = false;
            inputRowLayout.childForceExpandHeight = false;
            inputRowLayout.childAlignment = TextAnchor.MiddleLeft;

            var input = CreateInput(inputRow.transform, "125000", "Örnek bütçe");
            input.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 40f);
            var inputLayout = input.gameObject.AddComponent<LayoutElement>();
            inputLayout.preferredWidth = 200f;
            inputLayout.minWidth = 200f;
            inputLayout.preferredHeight = 40f;
            inputLayout.minHeight = 40f;

            var buttonRow = CreateHorizontalGroup(content.transform, "ControlButtons", new RectOffset(0, 0, 0, 0), 10f);
            buttonRow.AddComponent<LayoutElement>().preferredHeight = 46f;
            var buttonRowLayout = buttonRow.GetComponent<HorizontalLayoutGroup>();
            buttonRowLayout.childControlWidth = true;
            buttonRowLayout.childControlHeight = true;
            buttonRowLayout.childForceExpandWidth = false;
            buttonRowLayout.childForceExpandHeight = false;
            buttonRowLayout.childAlignment = TextAnchor.MiddleLeft;

            var primaryButton = CreateStyledButton(buttonRow.transform, "PrimarySandboxAction", "Primary Action", ColBlue, Blend(ColBlue, ColCyan, 0.28f), Darken(ColBlue, 0.22f));
            var primaryLayout = primaryButton.gameObject.AddComponent<LayoutElement>();
            primaryLayout.preferredWidth = bottomButtonWidth;
            primaryLayout.minWidth = bottomButtonWidth;
            primaryLayout.preferredHeight = 40f;
            primaryLayout.minHeight = 40f;

            var secondaryButton = CreateStyledButton(buttonRow.transform, "SecondarySandboxAction", "Secondary Action", ColSurfaceAlt, Blend(ColSurfaceAlt, ColBlue, 0.15f), Darken(ColSurfaceAlt, 0.12f));
            var secondaryLayout = secondaryButton.gameObject.AddComponent<LayoutElement>();
            secondaryLayout.preferredWidth = bottomButtonWidth;
            secondaryLayout.minWidth = bottomButtonWidth;
            secondaryLayout.preferredHeight = 40f;
            secondaryLayout.minHeight = 40f;
        }

        private void CreateSectorPreviewCard(Transform parent, string title, int activeJobs, int payoutDays, string riskLabel, Color accent)
        {
            var card = CreateSurface(parent, title.Replace(' ', '_') + "Card", 186f, ColPanel);
            card.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 186f);
            var cardLayout = card.GetComponent<LayoutElement>();
            cardLayout.preferredWidth = 400f;
            cardLayout.preferredHeight = 186f;
            cardLayout.minWidth = 400f;
            cardLayout.minHeight = 186f;
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
            topRow.AddComponent<LayoutElement>().preferredHeight = 44f;
            var topLayout = topRow.AddComponent<HorizontalLayoutGroup>();
            topLayout.spacing = 0f;
            topLayout.childControlWidth = true;
            topLayout.childControlHeight = true;
            topLayout.childForceExpandWidth = true;
            topLayout.childForceExpandHeight = false;
            topLayout.childAlignment = TextAnchor.MiddleCenter;

            var riskTag = CreateTag(topRow.transform, riskLabel, new Color(accent.r, accent.g, accent.b, 0.18f), accent, 14);
            var riskTagLayout = riskTag.GetComponent<LayoutElement>();
            if (riskTagLayout != null)
            {
                riskTagLayout.flexibleWidth = 0f;
            }

            var titleText = CreateText(content.transform, title, 20, TextAnchor.UpperLeft);
            titleText.color = ColText;
            titleText.fontStyle = FontStyle.Bold;
            titleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;

            var subtitle = CreateText(content.transform, $"Gelir {payoutDays} günde bir • Aktif iş {activeJobs}", 12, TextAnchor.MiddleLeft);
            subtitle.color = ColMuted;
            subtitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

            CreateFlexibleSpacer(content.transform);

            var metricRow = CreateUiObject("Metrics", content.transform);
            metricRow.AddComponent<LayoutElement>().preferredHeight = 46f;
            var metricGrid = metricRow.AddComponent<GridLayoutGroup>();
            metricGrid.cellSize = new Vector2(184f, 46f);
            metricGrid.spacing = new Vector2(8f, 0f);
            metricGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            metricGrid.constraintCount = 2;
            metricGrid.childAlignment = TextAnchor.MiddleCenter;

            CreateMiniStat(metricRow.transform, activeJobs.ToString(), "Aktif İş");
            CreateMiniStat(metricRow.transform, payoutDays + "g", "Döngü");
        }

        private void CreateProjectPreviewSampleCard(Transform parent, string titleValue, string cycleValue, string revenueValue, string profitValue, string employeesValue, Color accent)
        {
            var card = CreateSurface(parent, titleValue.Replace(' ', '_') + "ProjectCard", 228f, ColPanel);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(400f, 228f);
            var cardLayout = card.GetComponent<LayoutElement>();
            cardLayout.preferredWidth = 400f;
            cardLayout.preferredHeight = 228f;
            cardLayout.minWidth = 400f;
            cardLayout.minHeight = 228f;
            CreateAccentBar(card.transform, accent);

            var content = CreateStretchContainer(card.transform, "Content", 12f, 12f, 12f, 12f);
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 0, 0, 0);
            contentLayout.spacing = 8f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var title = CreateText(content.transform, titleValue, 22, TextAnchor.MiddleLeft);
            title.color = ColText;
            title.fontStyle = FontStyle.Bold;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

            var statRow = CreateUiObject("ProjectStats", content.transform);
            statRow.AddComponent<LayoutElement>().preferredHeight = 42f;
            var statGrid = statRow.AddComponent<GridLayoutGroup>();
            statGrid.cellSize = new Vector2(116f, 42f);
            statGrid.spacing = new Vector2(6f, 0f);
            statGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            statGrid.constraintCount = 3;
            statGrid.childAlignment = TextAnchor.MiddleCenter;

            CreateMiniStat(statRow.transform, cycleValue, "Gelir Döngüsü");
            CreateMiniStat(statRow.transform, revenueValue, "Gelir");
            CreateMiniStat(statRow.transform, profitValue, "Kâr");

            var employees = CreateText(content.transform, $"Çalışanlar: {employeesValue}", 14, TextAnchor.MiddleLeft);
            employees.color = ColMuted;
            employees.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

            CreateFlexibleSpacer(content.transform);

            var buttonRow = CreateUiObject("ActionRow", content.transform);
            buttonRow.AddComponent<LayoutElement>().preferredHeight = 80f;
            var buttonLayout = buttonRow.AddComponent<VerticalLayoutGroup>();
            buttonLayout.spacing = 8f;
            buttonLayout.padding = new RectOffset(0, 0, 0, 0);
            buttonLayout.childControlWidth = true;
            buttonLayout.childControlHeight = true;
            buttonLayout.childForceExpandWidth = true;
            buttonLayout.childForceExpandHeight = false;

            var editButton = CreateStyledButton(buttonRow.transform, titleValue.Replace(' ', '_') + "EditButton", "Düzenle", ColBlue, Blend(ColBlue, ColCyan, 0.3f), Darken(ColBlue, 0.25f));
            editButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;

            var sellButton = CreateStyledButton(buttonRow.transform, titleValue.Replace(' ', '_') + "SellButton", "Sat (288,000)", new Color(ColRed.r, ColRed.g, ColRed.b, 0.16f), new Color(ColRed.r, ColRed.g, ColRed.b, 0.26f), new Color(ColRed.r, ColRed.g, ColRed.b, 0.34f), ColRed);
            sellButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
        }

        private void CreateEmployeeSlotCard(Transform parent, float width, float height, bool isExpanded)
        {
            var card = CreateUiObject("EmployeeSlotCard", parent);
            ApplyRoundedImage(card, ColPanel);
            AddHoverEffect(card, ColPanel, Blend(ColPanel, ColBlue, 0.12f));

            var rect = card.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);

            var layout = card.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;
            layout.minWidth = width;
            layout.minHeight = height;

            var button = card.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.colors = CreateButtonColors(ColPanel, Blend(ColPanel, ColBlue, 0.12f), Darken(ColPanel, 0.08f));
            button.onClick.AddListener(() =>
            {
                showEmployeeCandidates = !showEmployeeCandidates;
                RefreshPage();
            });

            CreateAccentBar(card.transform, ColBlue);

            var content = CreateStretchContainer(card.transform, "Content", 16f, 12f, 16f, 12f);
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 0, 0, 0);
            contentLayout.spacing = 6f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var topRow = CreateUiObject("TopRow", content.transform);
            topRow.AddComponent<LayoutElement>().preferredHeight = 26f;
            var topRowLayout = topRow.AddComponent<HorizontalLayoutGroup>();
            topRowLayout.spacing = 8f;
            topRowLayout.childControlWidth = true;
            topRowLayout.childControlHeight = true;
            topRowLayout.childForceExpandWidth = true;
            topRowLayout.childForceExpandHeight = false;
            topRowLayout.childAlignment = TextAnchor.MiddleLeft;

            var title = CreateText(topRow.transform, "Örnek Çalışan Slotu", 18, TextAnchor.MiddleLeft);
            title.color = ColText;
            title.fontStyle = FontStyle.Bold;

            CreateTag(topRow.transform, isExpanded ? "Açık" : "Kapalı", new Color(ColBlue.r, ColBlue.g, ColBlue.b, 0.18f), ColBlue);

            var subtitle = CreateText(content.transform, "Senior Backend Developer • Tıklayınca aday kartları açılır", 14, TextAnchor.MiddleLeft);
            subtitle.color = ColMuted;
            subtitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

            var info = CreateText(content.transform, isExpanded ? "Aday listesi görüntüleniyor" : "Adayları görmek için tıkla", 13, TextAnchor.MiddleLeft);
            info.color = new Color(ColText.r, ColText.g, ColText.b, 0.75f);
            info.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        }

        private void CreateEmployeeCandidateSampleCard(Transform parent, string nameValue, string roleValue, string levelValue, string salaryValue, Color accent)
        {
            var card = CreateSurface(parent, nameValue.Replace(' ', '_') + "EmployeeCard", 128f, ColPanel);
            var rect = card.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400f, 128f);

            var layout = card.GetComponent<LayoutElement>();
            layout.preferredWidth = 400f;
            layout.preferredHeight = 128f;
            layout.minWidth = 400f;
            layout.minHeight = 128f;

            CreateAccentBar(card.transform, accent);

            var content = CreateStretchContainer(card.transform, "Content", 12f, 12f, 12f, 12f);
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 0, 0, 0);
            contentLayout.spacing = 6f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var topRow = CreateUiObject("TopRow", content.transform);
            topRow.AddComponent<LayoutElement>().preferredHeight = 24f;
            var topRowLayout = topRow.AddComponent<HorizontalLayoutGroup>();
            topRowLayout.spacing = 8f;
            topRowLayout.childControlWidth = true;
            topRowLayout.childControlHeight = true;
            topRowLayout.childForceExpandWidth = true;
            topRowLayout.childForceExpandHeight = false;
            topRowLayout.childAlignment = TextAnchor.MiddleLeft;

            var name = CreateText(topRow.transform, nameValue, 18, TextAnchor.MiddleLeft);
            name.color = ColText;
            name.fontStyle = FontStyle.Bold;

            CreateTag(topRow.transform, levelValue, new Color(accent.r, accent.g, accent.b, 0.18f), accent);

            var role = CreateText(content.transform, roleValue, 14, TextAnchor.MiddleLeft);
            role.color = ColMuted;
            role.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

            var statsRow = CreateUiObject("StatsRow", content.transform);
            statsRow.AddComponent<LayoutElement>().preferredHeight = 42f;
            var statsGrid = statsRow.AddComponent<GridLayoutGroup>();
            statsGrid.cellSize = new Vector2(184f, 42f);
            statsGrid.spacing = new Vector2(8f, 0f);
            statsGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            statsGrid.constraintCount = 2;
            statsGrid.childAlignment = TextAnchor.MiddleCenter;

            CreateMiniStat(statsRow.transform, salaryValue, "Maaş");
            CreateMiniStat(statsRow.transform, levelValue, "Seviye");
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

        private void CreateMetricCard(Transform parent, string title, string accentName, Color accent, string badge)
        {
            var card = CreateSurface(parent, title.Replace(' ', '_') + "Metric", 88f, ColSurface);
            CreateAccentBar(card.transform, accent);

            var content = CreateStretchContainer(card.transform, "Content", 12f, 10f, 12f, 12f);
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 0, 0, 0);
            contentLayout.spacing = 4f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var label = CreateText(content.transform, title, 14, TextAnchor.MiddleLeft);
            label.color = ColMuted;
            label.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

            var value = CreateText(content.transform, accentName, 20, TextAnchor.MiddleLeft);
            value.color = ColText;
            value.fontStyle = FontStyle.Bold;
            value.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

            CreateFlexibleSpacer(content.transform);

            CreateTag(content.transform, badge, new Color(accent.r, accent.g, accent.b, 0.18f), accent);
        }

        private GameObject CreateMiniStat(Transform parent, string value, string label)
        {
            var tile = CreateSurface(parent, "MiniStat", 46f, new Color(ColSurfaceAlt.r, ColSurfaceAlt.g, ColSurfaceAlt.b, 0.95f));
            var valueText = CreateText(tile.transform, value, 17, TextAnchor.UpperCenter);
            valueText.color = ColText;
            valueText.fontStyle = FontStyle.Bold;
            StretchToParent(valueText.rectTransform, 6f, 18f, 6f, 3f);

            var labelText = CreateText(tile.transform, label, 11, TextAnchor.LowerCenter);
            labelText.color = ColMuted;
            StretchToParent(labelText.rectTransform, 6f, 3f, 6f, 21f);
            return tile;
        }

        private Button CreateStyledButton(Transform parent, string objectName, string label, Color normal, Color hover, Color pressed)
        {
            return CreateStyledButton(parent, objectName, label, normal, hover, pressed, ColText);
        }

        private Button CreateStyledButton(Transform parent, string objectName, string label, Color normal, Color hover, Color pressed, Color textColor)
        {
            var buttonObject = CreateUiObject(objectName, parent);
            ApplyRoundedImage(buttonObject, normal);
            AddHoverEffect(buttonObject, normal, hover);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            button.colors = CreateButtonColors(normal, hover, pressed);

            var text = CreateText(buttonObject.transform, label, 18, TextAnchor.MiddleCenter);
            text.color = textColor;
            text.fontStyle = FontStyle.Bold;
            StretchToParent(text.rectTransform, 12f, 4f, 12f, 4f);
            return button;
        }

        private InputField CreateInput(Transform parent, string value, string placeholderValue)
        {
            var inputObject = CreateUiObject("InputField", parent);
            ApplyRoundedImage(inputObject, ColSurfaceAlt);

            var inputField = inputObject.AddComponent<InputField>();
            inputField.contentType = InputField.ContentType.Standard;
            inputField.lineType = InputField.LineType.SingleLine;
            inputField.text = value;

            var placeholder = CreateText(inputObject.transform, placeholderValue, 16, TextAnchor.MiddleLeft);
            placeholder.color = new Color(ColMuted.r, ColMuted.g, ColMuted.b, 0.45f);
            StretchToParent(placeholder.rectTransform, 14f, 8f, 14f, 8f);

            var text = CreateText(inputObject.transform, value, 16, TextAnchor.MiddleLeft);
            text.color = ColText;
            StretchToParent(text.rectTransform, 14f, 8f, 14f, 8f);

            inputField.placeholder = placeholder;
            inputField.textComponent = text;
            return inputField;
        }

        private GameObject CreateTag(Transform parent, string value, Color bgColor, Color textColor)
        {
            return CreateTag(parent, value, bgColor, textColor, 12);
        }

        private GameObject CreateTag(Transform parent, string value, Color bgColor, Color textColor, int fontSize)
        {
            var tag = CreateUiObject("Tag", parent);
            ApplyRoundedImage(tag, bgColor);
            tag.AddComponent<LayoutElement>().preferredHeight = fontSize >= 14 ? 30f : 26f;

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

        private GameObject CreateHorizontalGroup(Transform parent, string name, RectOffset padding, float spacing)
        {
            var row = CreateUiObject(name, parent);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = padding;
            layout.spacing = spacing;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleLeft;
            return row;
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

        private static void IgnoreLayout(GameObject target)
        {
            var layout = target.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = target.AddComponent<LayoutElement>();
            }

            layout.ignoreLayout = true;
        }

        private int CalculateGridColumnCount(float cardWidth, float spacing)
        {
            const float horizontalPadding = 80f;
            var availableWidth = Mathf.Max(cardWidth, panelSize.x - horizontalPadding);
            return Mathf.Max(1, Mathf.FloorToInt((availableWidth + spacing) / (cardWidth + spacing)));
        }

        private static float CalculateGridHeight(int itemCount, int columnCount, float cardHeight, float spacing)
        {
            var safeColumnCount = Mathf.Max(1, columnCount);
            var rowCount = Mathf.CeilToInt(itemCount / (float)safeColumnCount);
            return rowCount * cardHeight + Mathf.Max(0, rowCount - 1) * spacing;
        }

        private void CreateSectionTitle(string title)
        {
            var sectionTitle = CreateText(contentRoot, title, 20, TextAnchor.MiddleLeft);
            sectionTitle.color = ColText;
            sectionTitle.fontStyle = FontStyle.Bold;
            sectionTitle.rectTransform.sizeDelta = new Vector2(0f, 34f);
            var layout = sectionTitle.gameObject.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = sectionTitle.gameObject.AddComponent<LayoutElement>();
            }

            layout.preferredHeight = 34f;
            layout.minHeight = 34f;
        }

        private void CreateOpenButton()
        {
            var button = RuntimePanelUiUtility.CreateDesktopAppButton(RuntimePanelUiUtility.GetOrCreateComputerDesktopIconRoot(rootCanvas), defaultFont, "UiLabOpenButton", "UI Lab", appIcon, "LAB", ColPurple);
            button.onClick.AddListener(OpenPanel);
        }

        private void CreatePanel()
        {
            panelRoot = CreateUiObject("DesignSandboxPanel", RuntimePanelUiUtility.GetOrCreateComputerWindowRoot(rootCanvas));
            var panelRect = panelRoot.GetComponent<RectTransform>();
            RuntimePanelUiUtility.ConfigureFillComputerPanelChild(panelRect, rootCanvas);
            ApplyRoundedImage(panelRoot, ColBg);
            EnsureRoundedMask(panelRoot);

            var header = CreateUiObject("Header", panelRoot.transform);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0f, 82f);
            ApplyRoundedImage(header, ColPanel);
            EnsureRoundedMask(header);

            var badge = CreateRoundedBlock(header.transform, "HeaderBadge", new Vector2(48f, 48f), new Color(ColCyan.r, ColCyan.g, ColCyan.b, 0.18f));
            var badgeRect = badge.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0f, 0.5f);
            badgeRect.anchorMax = new Vector2(0f, 0.5f);
            badgeRect.pivot = new Vector2(0f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(18f, 0f);
            var badgeText = CreateText(badge.transform, "LAB", 16, TextAnchor.MiddleCenter);
            badgeText.color = ColCyan;
            badgeText.fontStyle = FontStyle.Bold;
            StretchToParent(badgeText.rectTransform, 0f, 0f, 0f, 0f);

            pageTitleText = CreateText(header.transform, "UI Lab", 24, TextAnchor.MiddleLeft);
            pageTitleText.color = ColText;
            pageTitleText.fontStyle = FontStyle.Bold;
            pageTitleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            pageTitleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            pageTitleText.rectTransform.offsetMin = new Vector2(86f, -40f);
            pageTitleText.rectTransform.offsetMax = new Vector2(-140f, -12f);

            pageSubtitleText = CreateText(header.transform, string.Empty, 14, TextAnchor.MiddleLeft);
            pageSubtitleText.color = ColMuted;
            pageSubtitleText.rectTransform.anchorMin = new Vector2(0f, 0f);
            pageSubtitleText.rectTransform.anchorMax = new Vector2(1f, 0f);
            pageSubtitleText.rectTransform.offsetMin = new Vector2(86f, 12f);
            pageSubtitleText.rectTransform.offsetMax = new Vector2(-140f, 34f);

            var closeButton = CreateStyledButton(header.transform, "CloseButton", "×", new Color(ColRed.r, ColRed.g, ColRed.b, 0.16f), new Color(ColRed.r, ColRed.g, ColRed.b, 0.28f), new Color(ColRed.r, ColRed.g, ColRed.b, 0.4f), ColRed);
            var closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-18f, 0f);
            closeRect.sizeDelta = new Vector2(50f, 44f);
            closeButton.onClick.AddListener(ClosePanel);

            var scrollRoot = CreateUiObject("ScrollRoot", panelRoot.transform);
            var scrollRootRect = scrollRoot.GetComponent<RectTransform>();
            scrollRootRect.anchorMin = new Vector2(0f, 0f);
            scrollRootRect.anchorMax = new Vector2(1f, 1f);
            scrollRootRect.offsetMin = new Vector2(14f, 14f);
            scrollRootRect.offsetMax = new Vector2(-14f, -94f);
            ApplyRoundedImage(scrollRoot, new Color(ColPanel.r, ColPanel.g, ColPanel.b, 0.72f));
            EnsureRoundedMask(scrollRoot);

            var scrollRect = scrollRoot.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;

            var viewport = CreateUiObject("Viewport", scrollRoot.transform);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(6f, 6f);
            viewportRect.offsetMax = new Vector2(-6f, -6f);
            viewport.AddComponent<RectMask2D>();
            viewport.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);

            var content = CreateUiObject("Content", viewport.transform);
            contentRoot = content.GetComponent<RectTransform>();
            contentRoot.anchorMin = new Vector2(0f, 1f);
            contentRoot.anchorMax = new Vector2(1f, 1f);
            contentRoot.pivot = new Vector2(0.5f, 1f);
            contentRoot.anchoredPosition = Vector2.zero;
            contentRoot.sizeDelta = new Vector2(0f, 0f);

            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(8, 8, 8, 8);
            contentLayout.spacing = 36f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var contentFitter = content.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRoot;
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

        private GameObject CreateUiObject(string objectName, Transform parent)
        {
            return RuntimePanelUiUtility.CreateUiObject(objectName, parent);
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
    }
}
