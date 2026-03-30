using CompanySimulator.Features.Sectors.Runtime.Definitions;
using CompanySimulator.Shared.Runtime.Economy;

namespace CompanySimulator.Features.Rivals.Runtime.Models
{
    public readonly struct RivalJobLogEntry
    {
        public RivalJobLogEntry(string jobName, SectorDefinition sector, int day, Money amount)
        {
            JobName = jobName;
            Sector = sector;
            Day = day;
            Amount = amount;
        }

        public string JobName { get; }
        public SectorDefinition Sector { get; }
        public int Day { get; }
        public Money Amount { get; }
    }
}
