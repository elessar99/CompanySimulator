using CompanySimulator.Features.Finance.Runtime.Definitions;

namespace CompanySimulator.Features.Finance.Runtime.Models
{
    public readonly struct ProjectExecutionHistoryEntry
    {
        // Ekonomide tamamlanan her işin sonucunu daha sonra tekrar okuyabilmek için saklar.
        public ProjectExecutionHistoryEntry(ProjectExecutionDefinition executionDefinition, ProjectEconomyResult result)
        {
            ExecutionDefinition = executionDefinition;
            Result = result;
        }

        public ProjectExecutionDefinition ExecutionDefinition { get; }
        public ProjectEconomyResult Result { get; }
    }
}
