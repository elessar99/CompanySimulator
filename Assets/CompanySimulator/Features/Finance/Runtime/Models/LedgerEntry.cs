using CompanySimulator.Shared.Runtime.Economy;

namespace CompanySimulator.Features.Finance.Runtime.Models
{
    public readonly struct LedgerEntry
    {
        public LedgerEntry(int day, LedgerEntryType type, Money amount, string description)
        {
            Day = day;
            Type = type;
            Amount = amount;
            Description = description ?? string.Empty;
        }

        public int Day { get; }
        public LedgerEntryType Type { get; }
        public Money Amount { get; }
        public string Description { get; }
    }
}
