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

        public IReadOnlyList<LoanOfferDefinition> StandardOffers => standardOffers;
        public IReadOnlyList<DynamicLoanOfferTemplateDefinition> SpecialOfferTemplates => specialOfferTemplates;
    }
}
