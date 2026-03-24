using CompanySimulator.Shared.Runtime.Economy;

namespace CompanySimulator.Features.FinanceOverview.Runtime.Models
{
    public readonly struct FinanceLineItemSnapshot
    {
        public FinanceLineItemSnapshot(string title, Money amount, string detail)
        {
            Title = title ?? string.Empty;
            Amount = amount;
            Detail = detail ?? string.Empty;
        }

        public string Title { get; }
        public Money Amount { get; }
        public string Detail { get; }
    }
}
