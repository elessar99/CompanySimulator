using CompanySimulator.Features.Banking.Runtime.Components;
using CompanySimulator.Features.Banking.Runtime.Models;
using CompanySimulator.Presentation.UI.Runtime.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CompanySimulator.Presentation.UI.Runtime.Components
{
    [DisallowMultipleComponent]
    public sealed class BankPanelUI : MonoBehaviour
    {
        [SerializeField] private CompanyBankManager companyBankManager;
        [SerializeField] private SectorPanelUI sectorPanelUI;
        [SerializeField] private EmployeePanelUI employeePanelUI;
        [SerializeField] private AccountingPanelUI accountingPanelUI;
        [SerializeField] private FinanceOverviewPanelUI financeOverviewPanelUI;
        [SerializeField] private RivalCompanyPanelUI rivalCompanyPanelUI;
        [SerializeField] private DebugPanelUI debugPanelUI;
        [SerializeField] private SecurityPanelUI securityPanelUI;
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private Vector2 panelSize = new Vector2(720f, 700f);

        private Font defaultFont;
        private GameObject panelRoot;
        private RectTransform contentRoot;
        private Text pageTitleText;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            companyBankManager ??= FindObjectOfType<CompanyBankManager>();
            if (companyBankManager == null)
            {
                companyBankManager = new GameObject("CompanyBankManager", typeof(CompanyBankManager)).GetComponent<CompanyBankManager>();
            }

            sectorPanelUI ??= FindObjectOfType<SectorPanelUI>();
            employeePanelUI ??= FindObjectOfType<EmployeePanelUI>();
            accountingPanelUI ??= FindObjectOfType<AccountingPanelUI>();
            financeOverviewPanelUI ??= FindObjectOfType<FinanceOverviewPanelUI>();
            rivalCompanyPanelUI ??= FindObjectOfType<RivalCompanyPanelUI>();
            debugPanelUI ??= FindObjectOfType<DebugPanelUI>();
            securityPanelUI ??= FindObjectOfType<SecurityPanelUI>();
            EnsureCanvas();
            EnsureEventSystem();
            defaultFont = LoadDefaultFont();
            BuildUi();
        }

        private void OnEnable()
        {
            if (companyBankManager != null)
            {
                companyBankManager.DataChanged -= RefreshPage;
                companyBankManager.DataChanged += RefreshPage;
            }

            RefreshPage();
        }

        private void OnDisable()
        {
            if (companyBankManager != null)
            {
                companyBankManager.DataChanged -= RefreshPage;
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

            if (securityPanelUI != null && securityPanelUI.IsOpen)
            {
                securityPanelUI.ClosePanel();
            }

            panelRoot.SetActive(true);
            RefreshPage();
        }

        public void ClosePanel()
        {
            panelRoot.SetActive(false);
        }

        private void RefreshPage()
        {
            if (contentRoot == null || companyBankManager == null)
            {
                return;
            }

            if (!companyBankManager.IsInitialized)
            {
                companyBankManager.Initialize();
            }

            pageTitleText.text = "Banka";
            RuntimePanelUiUtility.ClearChildren(contentRoot);

            CreateInfoCard($"30 Günlük Aktif İş Geliri: {companyBankManager.GetMonthlyActiveProjectRevenue().Amount:N0}", 62f);
            CreateInfoCard($"Özel Teklif Hesap Bakiyesi: {companyBankManager.GetAdjustedBalanceForSpecialOfferCalculation().Amount:N0}\nToplam Kalan Kredi Borcu: {companyBankManager.GetTotalOutstandingDebt().Amount:N0}", 82f);
            if (!string.IsNullOrWhiteSpace(companyBankManager.LastBankSummary))
            {
                CreateInfoCard(companyBankManager.LastBankSummary, 72f);
            }

            CreateSectionTitle("Standart Krediler");
            RenderOffers(companyBankManager.GetStandardOffers());

            CreateSectionTitle("Özel Teklifler");
            RenderOffers(companyBankManager.GetSpecialOffers());

            CreateSectionTitle("Aktif Krediler");
            RenderActiveLoans();
        }

        private void RenderOffers(System.Collections.Generic.IReadOnlyList<LoanOfferSnapshot> offers)
        {
            if (offers.Count == 0)
            {
                CreateInfoCard("Gösterilecek kredi teklifi yok.", 58f);
                return;
            }

            for (var i = 0; i < offers.Count; i++)
            {
                CreateOfferCard(offers[i]);
            }
        }

        private void RenderActiveLoans()
        {
            var activeLoans = companyBankManager.ActiveLoans;
            if (activeLoans.Count == 0)
            {
                CreateInfoCard("Aktif kredi bulunmuyor.", 58f);
                return;
            }

            for (var i = 0; i < activeLoans.Count; i++)
            {
                var loan = activeLoans[i];
                var closureAmount = loan.GetEarlyClosureAmount();
                CreateInfoCard($"{loan.DisplayName}\nKalan Borç: {loan.RemainingDebt.Amount:N0}\nKalan Anapara: {loan.RemainingPrincipalAmount.Amount:N0}\nTaksit: {loan.BuildCurrentInstallmentAmount().Amount:N0} | Kalan Taksit: {loan.RemainingInstallmentCount}\nSonraki Ödeme Günü: {loan.NextDueDay}\nErken Kapatma: {closureAmount.Amount:N0}", 136f);

                var closeButton = CreateButton(contentRoot, $"CloseLoan_{loan.OfferId}_{i}", $"Krediyi Kapat ({closureAmount.Amount:N0})");
                closeButton.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 54f);
                closeButton.onClick.AddListener(() =>
                {
                    if (companyBankManager.TryCloseLoan(loan, out _))
                    {
                        RefreshPage();
                    }
                });
            }
        }

        private void CreateOfferCard(LoanOfferSnapshot offer)
        {
            CreateInfoCard($"{offer.DisplayName}\nTutar: {offer.PrincipalAmount.Amount:N0}\nFaiz: %{offer.InterestRate * 100f:0.0}\nTaksit Aralığı: {offer.InstallmentIntervalDays} gün | Süre: {offer.TotalTermDays} gün", 108f);

            var button = CreateButton(contentRoot, $"Offer_{offer.OfferId}", offer.CanAccept ? "Krediyi Al" : "Şu Anda Alınamaz");
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 54f);
            button.interactable = offer.CanAccept;
            button.onClick.AddListener(() =>
            {
                if (companyBankManager.TryAcceptOffer(offer, out _))
                {
                    RefreshPage();
                }
            });

            if (!offer.CanAccept && !string.IsNullOrWhiteSpace(offer.ValidationMessage))
            {
                CreateInfoCard(offer.ValidationMessage, 62f);
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
            var button = CreateButton(rootCanvas.transform, "BankOpenButton", "Banka");
            var buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 1f);
            buttonRect.anchorMax = new Vector2(0f, 1f);
            buttonRect.pivot = new Vector2(0f, 1f);
            buttonRect.anchoredPosition = new Vector2(620f, -80f);
            buttonRect.sizeDelta = new Vector2(180f, 44f);
            button.onClick.AddListener(OpenPanel);
        }

        private void CreatePanel()
        {
            panelRoot = CreateUiObject("BankPanel", rootCanvas.transform);
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

            pageTitleText = CreateText(headerRoot.transform, "Banka", 28, TextAnchor.MiddleLeft);
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
