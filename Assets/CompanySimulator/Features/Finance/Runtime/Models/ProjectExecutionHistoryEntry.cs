using CompanySimulator.Features.Finance.Runtime.Definitions;
using CompanySimulator.Features.Projects.Runtime.Definitions;
using CompanySimulator.Features.Sectors.Runtime.Definitions;
using CompanySimulator.Shared.Runtime.Economy;

namespace CompanySimulator.Features.Finance.Runtime.Models
{
    public readonly struct ProjectExecutionHistoryEntry
    {
        // Ekonomide tamamlanan her işin sonucunu daha sonra tekrar okuyabilmek için saklar.
        public ProjectExecutionHistoryEntry(ProjectExecutionDefinition executionDefinition, string displayName, ProjectTypeDefinition projectType, ProjectEconomyResult result)
        {
            SourceDefinition = executionDefinition;
            DisplayName = displayName;
            ProjectType = projectType;
            Result = result;
        }

        public ProjectExecutionDefinition SourceDefinition { get; }
        public string DisplayName { get; }
        public ProjectTypeDefinition ProjectType { get; }
        public ProjectEconomyResult Result { get; }
    }

    public sealed class ActiveProjectRuntimeEntry
    {
        public ActiveProjectRuntimeEntry(ProjectExecutionDefinition sourceDefinition, string displayName, ProjectTypeDefinition projectType, ProjectEconomyResult startupResult, int startedDay)
        {
            SourceDefinition = sourceDefinition;
            DisplayName = displayName;
            ProjectType = projectType;
            StartupResult = startupResult;
            StartedDay = startedDay;

            var sector = projectType != null ? projectType.Sector : null;
            PayoutIntervalDays = sector != null ? sector.ProfitPayoutIntervalDays : 1;
            NextPayoutDay = startedDay + PayoutIntervalDays;
        }

        public ProjectExecutionDefinition SourceDefinition { get; }
        public string DisplayName { get; }
        public ProjectTypeDefinition ProjectType { get; }
        public ProjectEconomyResult StartupResult { get; }
        public int StartedDay { get; }
        public int PayoutIntervalDays { get; }
        public int NextPayoutDay { get; private set; }
        public int PayoutCount { get; private set; }
        public SectorDefinition Sector => ProjectType != null ? ProjectType.Sector : null;
        public Money CyclePayrollCost => StartupResult.PayrollCost;
        public Money CycleRecurringInvestmentCost => StartupResult.RecurringInvestmentCost;
        public Money CycleRevenue => StartupResult.Revenue;
        public Money CycleProfit => CycleRevenue - CyclePayrollCost - CycleRecurringInvestmentCost;

        public void RegisterPayout()
        {
            PayoutCount++;
            NextPayoutDay += PayoutIntervalDays;
        }
    }
}
