using System.Collections.Generic;
using CompanySimulator.Shared.Runtime.Economy;

namespace CompanySimulator.Features.FinanceOverview.Runtime.Models
{
    public readonly struct FinanceForecastSnapshot
    {
        public FinanceForecastSnapshot(int referenceDay, IReadOnlyList<FinanceLineItemSnapshot> items, Money totalAmount)
        {
            ReferenceDay = referenceDay;
            Items = items;
            TotalAmount = totalAmount;
        }

        public int ReferenceDay { get; }
        public IReadOnlyList<FinanceLineItemSnapshot> Items { get; }
        public Money TotalAmount { get; }
    }
}
