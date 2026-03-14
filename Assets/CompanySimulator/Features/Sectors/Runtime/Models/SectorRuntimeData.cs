using System.Collections.Generic;
using CompanySimulator.Features.Finance.Runtime.Definitions;
using CompanySimulator.Features.Sectors.Runtime.Definitions;

namespace CompanySimulator.Features.Sectors.Runtime.Models
{
    public sealed class SectorRuntimeData
    {
        private readonly List<ProjectExecutionDefinition> availableProjects = new List<ProjectExecutionDefinition>(8);
        private readonly Dictionary<ProjectExecutionDefinition, int> completedProjectCounts = new Dictionary<ProjectExecutionDefinition, int>(8);

        // Her sektör için panelde gösterilecek çalışma verisini tek yerde toplar.
        public SectorRuntimeData(SectorDefinition sector)
        {
            Sector = sector;
        }

        public SectorDefinition Sector { get; }
        public IReadOnlyList<ProjectExecutionDefinition> AvailableProjects => availableProjects;
        public int CompletedProjectCount { get; private set; }

        public void AddProject(ProjectExecutionDefinition project)
        {
            if (project == null)
            {
                return;
            }

            if (!availableProjects.Contains(project))
            {
                availableProjects.Add(project);
            }

            if (!completedProjectCounts.ContainsKey(project))
            {
                completedProjectCounts.Add(project, 0);
            }
        }

        public void ResetProgress()
        {
            CompletedProjectCount = 0;

            var projectCount = availableProjects.Count;
            for (var i = 0; i < projectCount; i++)
            {
                completedProjectCounts[availableProjects[i]] = 0;
            }
        }

        public void RegisterCompletedProject(ProjectExecutionDefinition project)
        {
            if (project == null)
            {
                return;
            }

            AddProject(project);
            CompletedProjectCount++;
            completedProjectCounts[project] = completedProjectCounts[project] + 1;
        }

        public int GetCompletedCount(ProjectExecutionDefinition project)
        {
            if (project == null)
            {
                return 0;
            }

            return completedProjectCounts.TryGetValue(project, out var completedCount) ? completedCount : 0;
        }
    }
}
