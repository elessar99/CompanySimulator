using System;
using CompanySimulator.Features.Investments.Runtime.Definitions;
using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;

namespace CompanySimulator.Features.Finance.Runtime.Models
{
    [Serializable]
    public struct InvestmentAllocationInput
    {
        [SerializeField] private InvestmentTypeDefinition investmentType;
        [SerializeField, Min(0)] private int allocatedBudget;

        public InvestmentAllocationInput(InvestmentTypeDefinition investmentType, int allocatedBudget)
        {
            this.investmentType = investmentType;
            this.allocatedBudget = allocatedBudget;
        }

        public InvestmentTypeDefinition InvestmentType => investmentType;
        public Money AllocatedBudget => Money.From(allocatedBudget);
        public int AllocatedBudgetAmount => Mathf.Max(0, allocatedBudget);
    }
}
