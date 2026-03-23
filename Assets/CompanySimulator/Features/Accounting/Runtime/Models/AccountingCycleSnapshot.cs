using CompanySimulator.Shared.Runtime.Economy;

namespace CompanySimulator.Features.Accounting.Runtime.Models
{
    public readonly struct AccountingCycleSnapshot
    {
        public AccountingCycleSnapshot(
            int cycleStartDay,
            int nextTaxDay,
            int daysUntilTaxPayment,
            int activeProjectCount,
            int maxActiveProjectCount,
            Money income,
            Money expenses,
            Money profit,
            Money estimatedTax,
            Money lastTaxPayment)
        {
            CycleStartDay = cycleStartDay;
            NextTaxDay = nextTaxDay;
            DaysUntilTaxPayment = daysUntilTaxPayment;
            ActiveProjectCount = activeProjectCount;
            MaxActiveProjectCount = maxActiveProjectCount;
            Income = income;
            Expenses = expenses;
            Profit = profit;
            EstimatedTax = estimatedTax;
            LastTaxPayment = lastTaxPayment;
        }

        public int CycleStartDay { get; }
        public int NextTaxDay { get; }
        public int DaysUntilTaxPayment { get; }
        public int ActiveProjectCount { get; }
        public int MaxActiveProjectCount { get; }
        public Money Income { get; }
        public Money Expenses { get; }
        public Money Profit { get; }
        public Money EstimatedTax { get; }
        public Money LastTaxPayment { get; }
    }
}
