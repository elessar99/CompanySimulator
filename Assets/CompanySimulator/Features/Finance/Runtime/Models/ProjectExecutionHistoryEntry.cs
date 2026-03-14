using CompanySimulator.Features.Finance.Runtime.Definitions;
using CompanySimulator.Features.Projects.Runtime.Definitions;

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
}
