using System;
using System.Collections.Generic;
using CompanySimulator.Shared.Runtime.Definitions;
using UnityEngine;

namespace CompanySimulator.Features.Banking.Runtime.Definitions
{
    [CreateAssetMenu(fileName = "BankingSetupDefinition", menuName = "Company Simulator/Definitions/Banking/Setup")]
    public sealed class BankingSetupDefinition : DefinitionBase
    {
        [SerializeField] private LoanOfferDefinition[] standardOffers = Array.Empty<LoanOfferDefinition>();
        [SerializeField] private DynamicLoanOfferTemplateDefinition[] specialOfferTemplates = Array.Empty<DynamicLoanOfferTemplateDefinition>();
        [SerializeField, Range(0f, 5f)] private float baseSpecialInterestRate = 0.2f;
        [SerializeField, Range(-1f, 1f)] private float wealthyCompanyInterestOffset = -0.03f;
        [SerializeField, Range(-1f, 1f)] private float poorCompanyInterestOffset = 0.05f;
        [SerializeField, Min(0f)] private float lowBalanceToRevenueRatio = 0.25f;
        [SerializeField, Min(0f)] private float highBalanceToRevenueRatio = 1.5f;
        [SerializeField, Min(1)] private int minimumSpecialOfferAmount = 100000;

        public IReadOnlyList<LoanOfferDefinition> StandardOffers => standardOffers;
        public IReadOnlyList<DynamicLoanOfferTemplateDefinition> SpecialOfferTemplates => specialOfferTemplates;
        public float BaseSpecialInterestRate => Mathf.Max(0f, baseSpecialInterestRate);
        public float WealthyCompanyInterestOffset => wealthyCompanyInterestOffset;
        public float PoorCompanyInterestOffset => poorCompanyInterestOffset;
        public float LowBalanceToRevenueRatio => Mathf.Max(0f, lowBalanceToRevenueRatio);
        public float HighBalanceToRevenueRatio => Mathf.Max(LowBalanceToRevenueRatio, highBalanceToRevenueRatio);
        public int MinimumSpecialOfferAmount => Mathf.Max(1, minimumSpecialOfferAmount);
    }
}
