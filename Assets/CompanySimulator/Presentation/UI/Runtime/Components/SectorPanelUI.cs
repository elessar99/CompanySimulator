using System;
using System.Collections.Generic;
using CompanySimulator.Features.Finance.Runtime.Components;
using CompanySimulator.Features.Finance.Runtime.Definitions;
using CompanySimulator.Features.Finance.Runtime.Models;
using CompanySimulator.Features.Investments.Runtime.Definitions;
using CompanySimulator.Features.Sectors.Runtime.Components;
using CompanySimulator.Features.Sectors.Runtime.Models;
using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CompanySimulator.Presentation.UI.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class SectorPanelUI : MonoBehaviour
    {
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private SectorManager sectorManager;
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private Vector2 panelSize = new Vector2(760f, 720f);

        private readonly Dictionary<InvestmentTypeDefinition, int> draftBudgetCache = new Dictionary<InvestmentTypeDefinition, int>(8);

        private Font defaultFont;
        private GameObject panelRoot;
        private RectTransform contentRoot;
        private Text balanceText;
        private Text pageTitleText;
        private Text draftResultText;
        private Button backButton;
        private SectorRuntimeData selectedSector;
        private ProjectExecutionDefinition selectedProjectTemplate;
        private PageState currentPage;

        private void Awake()
        {
            // Referanslar inspector'dan atanmadýysa sahneden otomatik bulunur.
            economyManager ??= FindObjectOfType<EconomyManager>();
            sectorManager ??= FindObjectOfType<SectorManager>();

            EnsureCanvas();
            EnsureEventSystem();
            defaultFont = LoadDefaultFont();
            BuildStaticUi();
        }

        private void OnEnable()
        {
            SubscribeEvents();
            RefreshAll();
        }

        private void Start()
        {
            RefreshAll();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        public void OpenPanel()
        {
            panelRoot.SetActive(true);
            NavigateToSectorList();
        }

        public void ClosePanel()
        {
            // Kapatýrken her zaman ana sektör listesine dönülür.
            NavigateToSectorList();
            panelRoot.SetActive(false);
        }

        private void GoBack()
        {
            switch (currentPage)
            {
                case PageState.NewJob:
                    ShowSectorDetails(selectedSector);
                    return;
                case PageState.SectorDetails:
                    NavigateToSectorList();
                    return;
                default:
                    return;
            }
        }

        private void NavigateToSectorList()
        {
            selectedSector = null;
            selectedProjectTemplate = null;
            currentPage = PageState.SectorList;
            UpdateHeaderButtons();
            ShowSectorList();
        }

        private void RefreshAll()
        {
            RefreshBalanceText();

            if (sectorManager == null)
            {
                return;
            }

            if (!sectorManager.IsInitialized)
            {
                sectorManager.Initialize();
            }

            switch (currentPage)
            {
                case PageState.SectorDetails:
                    ShowSectorDetails(selectedSector);
                    break;
                case PageState.NewJob:
                    ShowNewJobPage(selectedSector);
                    break;
                default:
                    ShowSectorList();
                    break;
            }
        }

        private void SubscribeEvents()
        {
            if (economyManager != null)
            {
                economyManager.BalanceChanged -= HandleBalanceChanged;
                economyManager.BalanceChanged += HandleBalanceChanged;
            }

            if (sectorManager != null)
            {
                sectorManager.DataChanged -= HandleSectorDataChanged;
                sectorManager.DataChanged += HandleSectorDataChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            if (economyManager != null)
            {
                economyManager.BalanceChanged -= HandleBalanceChanged;
            }

            if (sectorManager != null)
            {
                sectorManager.DataChanged -= HandleSectorDataChanged;
            }
        }

        private void HandleBalanceChanged(Money _)
        {
            RefreshBalanceText();
        }

        private void HandleSectorDataChanged()
        {
            RefreshAll();
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

        private void BuildStaticUi()
        {
            CreateBalanceWidget();
            CreateOpenButton();
            CreatePanel();
            panelRoot.SetActive(false);
        }

        private void CreateBalanceWidget()
        {
            var balanceRoot = CreateUiObject("MoneyBar", rootCanvas.transform);
            var balanceRect = balanceRoot.GetComponent<RectTransform>();
            balanceRect.anchorMin = new Vector2(0f, 1f);
            balanceRect.anchorMax = new Vector2(0f, 1f);
            balanceRect.pivot = new Vector2(0f, 1f);
            balanceRect.anchoredPosition = new Vector2(20f, -20f);
            balanceRect.sizeDelta = new Vector2(320f, 48f);

            var background = balanceRoot.AddComponent<Image>();
            background.color = new Color(0.12f, 0.12f, 0.16f, 0.92f);

            balanceText = CreateText(balanceRoot.transform, "Para: 0", 22, TextAnchor.MiddleLeft);
            StretchToParent(balanceText.rectTransform, 14f, 6f, 14f, 6f);
        }

        private void CreateOpenButton()
        {
            var button = CreateButton(rootCanvas.transform, "SectorsOpenButton", "Sektörler");
            var buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 1f);
            buttonRect.anchorMax = new Vector2(0f, 1f);
            buttonRect.pivot = new Vector2(0f, 1f);
            buttonRect.anchoredPosition = new Vector2(20f, -80f);
            buttonRect.sizeDelta = new Vector2(180f, 44f);
            button.onClick.AddListener(OpenPanel);
        }

        private void CreatePanel()
        {
            panelRoot = CreateUiObject("SectorPanel", rootCanvas.transform);
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

            var headerBackground = headerRoot.AddComponent<Image>();
            headerBackground.color = new Color(0.17f, 0.21f, 0.29f, 1f);

            pageTitleText = CreateText(headerRoot.transform, "Sektörler", 28, TextAnchor.MiddleLeft);
            StretchToParent(pageTitleText.rectTransform, 18f, 8f, 140f, 8f);

            backButton = CreateButton(headerRoot.transform, "BackButton", "?");
            var backRect = backButton.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(1f, 0.5f);
            backRect.anchorMax = new Vector2(1f, 0.5f);
            backRect.pivot = new Vector2(1f, 0.5f);
            backRect.anchoredPosition = new Vector2(-72f, 0f);
            backRect.sizeDelta = new Vector2(50f, 40f);
            backButton.onClick.AddListener(GoBack);

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

            var scrollImage = scrollRoot.AddComponent<Image>();
            scrollImage.color = new Color(0.13f, 0.15f, 0.19f, 0.92f);

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

            var viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);

            var content = CreateUiObject("Content", viewport.transform);
            contentRoot = content.GetComponent<RectTransform>();
            contentRoot.anchorMin = new Vector2(0f, 1f);
            contentRoot.anchorMax = new Vector2(1f, 1f);
            contentRoot.pivot = new Vector2(0.5f, 1f);
            contentRoot.anchoredPosition = Vector2.zero;
            contentRoot.sizeDelta = new Vector2(0f, 0f);

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRoot;

            UpdateHeaderButtons();
        }

        private void ShowSectorList()
        {
            currentPage = PageState.SectorList;
            UpdateHeaderButtons();
            pageTitleText.text = "Sektörler";
            draftResultText = null;
            ClearChildren(contentRoot);

            if (sectorManager == null || !sectorManager.IsInitialized)
            {
                CreateInfoCard("Sektör sistemi henüz hazýr deðil.");
                return;
            }

            var sectors = sectorManager.Sectors;
            if (sectors.Count == 0)
            {
                CreateInfoCard("Henüz listelenecek sektör bulunmuyor.");
                return;
            }

            for (var i = 0; i < sectors.Count; i++)
            {
                CreateSectorButton(sectors[i]);
            }
        }

        private void ShowSectorDetails(SectorRuntimeData sectorData)
        {
            if (contentRoot == null)
            {
                return;
            }

            if (sectorData == null)
            {
                NavigateToSectorList();
                return;
            }

            currentPage = PageState.SectorDetails;
            selectedSector = sectorData;
            UpdateHeaderButtons();
            pageTitleText.text = sectorData.Sector.DisplayName;
            draftResultText = null;
            ClearChildren(contentRoot);

            if (!string.IsNullOrWhiteSpace(sectorData.Sector.Description))
            {
                CreateInfoCard(sectorData.Sector.Description, 72f);
            }

            CreateInfoCard($"Toplam tamamlanan iþ: {sectorData.CompletedProjectCount}");
            CreateInfoCard($"Sektörde çalýþabilecek meslek sayýsý: {sectorData.Sector.SupportedRoles.Count}");

            var availableProjects = sectorData.AvailableProjects;
            if (availableProjects.Count == 0)
            {
                CreateInfoCard("Bu sektör için henüz iþ tanýmý yok.");
            }
            else
            {
                for (var i = 0; i < availableProjects.Count; i++)
                {
                    CreateProjectCard(sectorData, availableProjects[i]);
                }
            }

            var newJobButton = CreateButton(contentRoot, "NewJobButton", "+ Yeni Ýþ");
            newJobButton.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 62f);
            newJobButton.onClick.AddListener(() => ShowNewJobPage(sectorData));
        }

        private void ShowNewJobPage(SectorRuntimeData sectorData)
        {
            if (contentRoot == null)
            {
                return;
            }

            if (sectorData == null)
            {
                NavigateToSectorList();
                return;
            }

            currentPage = PageState.NewJob;
            selectedSector = sectorData;
            UpdateHeaderButtons();
            pageTitleText.text = sectorData.Sector.DisplayName + " / Yeni Ýþ";
            ClearChildren(contentRoot);

            if (selectedProjectTemplate == null || !ContainsProject(sectorData, selectedProjectTemplate))
            {
                selectedProjectTemplate = sectorData.AvailableProjects.Count > 0 ? sectorData.AvailableProjects[0] : null;
            }

            if (selectedProjectTemplate == null)
            {
                CreateInfoCard("Bu sektörde kullanýlabilecek hazýr iþ þablonu bulunmuyor.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(sectorData.Sector.Description))
            {
                CreateInfoCard(sectorData.Sector.Description, 72f);
            }

            CreateSectionTitle("Ýþ Þablonlarý");
            for (var i = 0; i < sectorData.AvailableProjects.Count; i++)
            {
                CreateProjectTemplateSelector(sectorData.AvailableProjects[i]);
            }

            CreateSectionTitle("Gerekli Çalýþanlar");
            CreateEmployeeRequirementCards(selectedProjectTemplate);

            CreateSectionTitle("Yatýrýmlar");
            CreateInvestmentEditors(selectedProjectTemplate);

            CreateSectionTitle("Önizleme");
            var previewButton = CreateButton(contentRoot, "PreviewButton", "Önizleme Yap");
            previewButton.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 56f);
            previewButton.onClick.AddListener(PreviewDraft);

            draftResultText = CreateInfoCard("Bütçeleri girip önizleme yapabilirsin.", 128f);

            var startButton = CreateButton(contentRoot, "StartButton", "Ýþi Baþlat");
            startButton.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 62f);
            startButton.onClick.AddListener(StartDraft);
        }

        private bool ContainsProject(SectorRuntimeData sectorData, ProjectExecutionDefinition project)
        {
            var projects = sectorData.AvailableProjects;
            for (var i = 0; i < projects.Count; i++)
            {
                if (projects[i] == project)
                {
                    return true;
                }
            }

            return false;
        }

        private void CreateSectorButton(SectorRuntimeData sectorData)
        {
            var label = $"{sectorData.Sector.DisplayName}\nTamamlanan Ýþ: {sectorData.CompletedProjectCount}";
            var button = CreateButton(contentRoot, $"Sector_{sectorData.Sector.Id}", label);
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 78f);
            button.onClick.AddListener(() => ShowSectorDetails(sectorData));
        }

        private void CreateProjectCard(SectorRuntimeData sectorData, ProjectExecutionDefinition project)
        {
            var completedCount = sectorData.GetCompletedCount(project);
            var resultPreview = economyManager != null ? economyManager.PreviewProject(project) : default;
            var label = $"{project.DisplayName}\nTamamlanma: {completedCount} | Tahmini Kâr: {resultPreview.Profit.Amount:N0}";
            var button = CreateButton(contentRoot, $"Project_{project.Id}", label);
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 84f);
            button.onClick.AddListener(() =>
            {
                selectedProjectTemplate = project;
                ShowNewJobPage(sectorData);
            });
        }

        private void CreateProjectTemplateSelector(ProjectExecutionDefinition project)
        {
            var selectedText = project == selectedProjectTemplate ? "Seçili" : "Seç";
            var button = CreateButton(contentRoot, $"Template_{project.Id}", $"{project.DisplayName}\nDurum: {selectedText}");
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 72f);
            button.onClick.AddListener(() =>
            {
                selectedProjectTemplate = project;
                ShowNewJobPage(selectedSector);
            });
        }

        private void CreateEmployeeRequirementCards(ProjectExecutionDefinition project)
        {
            var assignments = project.EmployeeAssignments;
            if (assignments.Count == 0)
            {
                CreateInfoCard("Bu iþ için çalýþan tanýmý bulunmuyor.");
                return;
            }

            for (var i = 0; i < assignments.Count; i++)
            {
                var assignment = assignments[i];
                var role = assignment.Role;
                if (role == null)
                {
                    continue;
                }

                var canWorkInSector = selectedSector != null && role.CanWorkInSector(selectedSector.Sector);
                var compatibilityText = canWorkInSector ? "Uygun" : "Bu sektörde çalýþamaz";
                CreateInfoCard(
                    $"{role.DisplayName}\nGerekli Kiþi: {assignment.Count} | Ortalama Kalite: {assignment.AverageQuality:0}\nTek çalýþan eþ zamanlý atama limiti: {role.MaxConcurrentAssignmentsPerEmployee}\nSektör uyumu: {compatibilityText}",
                    92f);
            }
        }

        private void CreateInvestmentEditors(ProjectExecutionDefinition project)
        {
            draftBudgetCache.Clear();

            var investments = project.ProjectType != null ? project.ProjectType.RecommendedInvestments : Array.Empty<InvestmentTypeDefinition>();
            if (investments.Count == 0 && selectedSector != null)
            {
                investments = selectedSector.Sector.AvailableInvestments;
            }

            if (investments.Count == 0)
            {
                CreateInfoCard("Bu iþ için yatýrým tanýmý bulunmuyor.");
                return;
            }

            for (var i = 0; i < investments.Count; i++)
            {
                var investment = investments[i];
                if (investment == null)
                {
                    continue;
                }

                CreateInvestmentEditor(project, investment);
            }
        }

        private void CreateInvestmentEditor(ProjectExecutionDefinition project, InvestmentTypeDefinition investment)
        {
            var card = CreateUiObject($"Investment_{investment.Id}", contentRoot);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(0f, 116f);

            var cardImage = card.AddComponent<Image>();
            cardImage.color = new Color(0.18f, 0.2f, 0.25f, 1f);

            var nameText = CreateText(card.transform, investment.DisplayName, 22, TextAnchor.UpperLeft);
            StretchToParent(nameText.rectTransform, 14f, 10f, 220f, 54f);

            var typeLabel = investment.IsRecurringExpense ? "Gelirden Düþer" : "Peþin";
            var detailText = CreateText(
                card.transform,
                $"Gider Tipi: {typeLabel}\nMinimum: {investment.MinimumBudget:N0} | Önerilen: {investment.RecommendedBudget:N0}",
                18,
                TextAnchor.UpperLeft);
            StretchToParent(detailText.rectTransform, 14f, 42f, 220f, 10f);

            var inputField = CreateInputField(card.transform, GetDefaultBudget(project, investment).ToString());
            var inputRect = inputField.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(1f, 0.5f);
            inputRect.anchorMax = new Vector2(1f, 0.5f);
            inputRect.pivot = new Vector2(1f, 0.5f);
            inputRect.anchoredPosition = new Vector2(-16f, 6f);
            inputRect.sizeDelta = new Vector2(180f, 40f);

            var evaluationText = CreateText(card.transform, string.Empty, 18, TextAnchor.MiddleRight);
            var evaluationRect = evaluationText.rectTransform;
            evaluationRect.anchorMin = new Vector2(1f, 0f);
            evaluationRect.anchorMax = new Vector2(1f, 0f);
            evaluationRect.pivot = new Vector2(1f, 0f);
            evaluationRect.anchoredPosition = new Vector2(-16f, 12f);
            evaluationRect.sizeDelta = new Vector2(220f, 30f);

            void RefreshEvaluation(string inputValue)
            {
                var parsedBudget = ParseBudget(inputValue, investment.MinimumBudget);
                draftBudgetCache[investment] = parsedBudget;
                evaluationText.text = $"Deðerlendirme: {investment.GetBudgetEvaluationLabel(parsedBudget)}";
            }

            RefreshEvaluation(inputField.text);
            inputField.onEndEdit.AddListener(RefreshEvaluation);
        }

        private void PreviewDraft()
        {
            if (draftResultText == null || economyManager == null)
            {
                return;
            }

            var request = BuildDraftRequest();
            if (request == null)
            {
                draftResultText.text = "Önizleme için geçerli bir iþ þablonu seçilmedi.";
                return;
            }

            var result = economyManager.PreviewProject(request);
            draftResultText.text = BuildResultSummary(result);
        }

        private void StartDraft()
        {
            if (draftResultText == null || economyManager == null)
            {
                return;
            }

            var request = BuildDraftRequest();
            if (request == null)
            {
                draftResultText.text = "Ýþ baþlatmak için geçerli bir iþ þablonu seçilmedi.";
                return;
            }

            var displayName = selectedProjectTemplate != null ? selectedProjectTemplate.DisplayName : request.ProjectType.DisplayName;
            if (economyManager.TryExecuteProject(selectedProjectTemplate, request, displayName, out var result))
            {
                ShowNewJobPage(selectedSector);
                if (draftResultText != null)
                {
                    draftResultText.text = "Ýþ baþlatýldý.\n\n" + BuildResultSummary(result);
                }
            }
            else
            {
                draftResultText.text = "Ýþ baþlatýlamadý. Muhtemelen bakiye yetersiz.\n\n" + BuildResultSummary(economyManager.PreviewProject(request));
            }
        }

        private ProjectEconomyRequest BuildDraftRequest()
        {
            if (selectedProjectTemplate == null)
            {
                return null;
            }

            var projectType = selectedProjectTemplate.ProjectType;
            if (projectType == null)
            {
                return null;
            }

            var investments = projectType.RecommendedInvestments.Count > 0
                ? projectType.RecommendedInvestments
                : selectedSector != null ? selectedSector.Sector.AvailableInvestments : Array.Empty<InvestmentTypeDefinition>();

            var allocationList = new List<InvestmentAllocationInput>(investments.Count);
            for (var i = 0; i < investments.Count; i++)
            {
                var investment = investments[i];
                if (investment == null)
                {
                    continue;
                }

                var budget = draftBudgetCache.TryGetValue(investment, out var cachedBudget)
                    ? cachedBudget
                    : GetDefaultBudget(selectedProjectTemplate, investment);
                allocationList.Add(new InvestmentAllocationInput(investment, budget));
            }

            return new ProjectEconomyRequest(
                projectType,
                selectedProjectTemplate.EmployeeAssignments,
                allocationList,
                selectedProjectTemplate.MarketDemandMultiplier,
                selectedProjectTemplate.CompetitorPressure);
        }

        private string BuildResultSummary(ProjectEconomyResult result)
        {
            return
                $"Tahmini Gelir: {result.Revenue.Amount:N0}\n" +
                $"Personel Gideri: {result.PayrollCost.Amount:N0}\n" +
                $"Peþin Yatýrým: {result.UpfrontInvestmentCost.Amount:N0}\n" +
                $"Gelirden Düþen Gider: {result.RecurringInvestmentCost.Amount:N0}\n" +
                $"Sabit Gider: {result.FixedCost.Amount:N0}\n" +
                $"Toplam Kâr: {result.Profit.Amount:N0}\n" +
                $"Baþarý Puaný: {result.SuccessScore:0.00}\n" +
                $"Süre: {result.DurationDays} gün";
        }

        private int GetDefaultBudget(ProjectExecutionDefinition project, InvestmentTypeDefinition investment)
        {
            var templateBudget = project.GetAllocatedBudgetFor(investment);
            if (templateBudget > 0)
            {
                return templateBudget;
            }

            return Mathf.Max(investment.MinimumBudget, investment.RecommendedBudget);
        }

        private int ParseBudget(string inputValue, int fallbackValue)
        {
            return int.TryParse(inputValue, out var parsedValue) && parsedValue >= 0 ? parsedValue : fallbackValue;
        }

        private void CreateSectionTitle(string title)
        {
            var titleText = CreateText(contentRoot, title, 24, TextAnchor.MiddleLeft);
            titleText.rectTransform.sizeDelta = new Vector2(0f, 36f);
            titleText.color = new Color(0.94f, 0.94f, 0.98f, 1f);
        }

        private Text CreateInfoCard(string message, float height = 58f)
        {
            var infoRoot = CreateUiObject("InfoCard", contentRoot);
            var rect = infoRoot.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, height);

            var image = infoRoot.AddComponent<Image>();
            image.color = new Color(0.18f, 0.2f, 0.25f, 1f);

            var text = CreateText(infoRoot.transform, message, 20, TextAnchor.MiddleLeft);
            StretchToParent(text.rectTransform, 14f, 6f, 14f, 6f);
            return text;
        }

        private GameObject CreateUiObject(string objectName, Transform parent)
        {
            var uiObject = new GameObject(objectName, typeof(RectTransform));
            uiObject.transform.SetParent(parent, false);
            return uiObject;
        }

        private Button CreateButton(Transform parent, string objectName, string label)
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

            var text = CreateText(buttonObject.transform, label, 22, TextAnchor.MiddleLeft);
            StretchToParent(text.rectTransform, 16f, 8f, 16f, 8f);

            return button;
        }

        private InputField CreateInputField(Transform parent, string initialValue)
        {
            var inputObject = CreateUiObject("InputField", parent);
            var image = inputObject.AddComponent<Image>();
            image.color = new Color(0.1f, 0.12f, 0.16f, 1f);

            var inputField = inputObject.AddComponent<InputField>();
            inputField.contentType = InputField.ContentType.IntegerNumber;
            inputField.lineType = InputField.LineType.SingleLine;
            inputField.text = initialValue;

            var placeholder = CreateText(inputObject.transform, "Bütçe", 18, TextAnchor.MiddleLeft);
            placeholder.color = new Color(1f, 1f, 1f, 0.35f);
            StretchToParent(placeholder.rectTransform, 10f, 6f, 10f, 6f);

            var text = CreateText(inputObject.transform, initialValue, 18, TextAnchor.MiddleLeft);
            StretchToParent(text.rectTransform, 10f, 6f, 10f, 6f);

            inputField.placeholder = placeholder;
            inputField.textComponent = text;
            return inputField;
        }

        private Text CreateText(Transform parent, string value, int fontSize, TextAnchor anchor)
        {
            var textObject = CreateUiObject("Text", parent);
            var text = textObject.AddComponent<Text>();
            text.font = defaultFont;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private void RefreshBalanceText()
        {
            if (balanceText == null)
            {
                return;
            }

            var balanceAmount = economyManager != null ? economyManager.Balance.Amount : 0L;
            balanceText.text = $"Para: {balanceAmount:N0}";
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
                backButton.gameObject.SetActive(currentPage != PageState.SectorList);
            }
        }

        private void StretchToParent(RectTransform rectTransform, float left, float bottom, float right, float top)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(left, bottom);
            rectTransform.offsetMax = new Vector2(-right, -top);
        }

        private void ClearChildren(RectTransform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }

        private enum PageState
        {
            SectorList = 0,
            SectorDetails = 1,
            NewJob = 2
        }
    }
}
