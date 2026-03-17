using System.Collections.Generic;
using CompanySimulator.Features.Finance.Runtime.Definitions;
using CompanySimulator.Features.Sectors.Runtime.Definitions;

namespace CompanySimulator.Features.Sectors.Runtime.Models
{
    public sealed class SectorRuntimeData
    {
        private readonly List<ProjectExecutionDefinition> availableProjects = new List<ProjectExecutionDefinition>(8);
        private readonly Dictionary<ProjectExecutionDefinition, int> activeProjectCounts = new Dictionary<ProjectExecutionDefinition, int>(8);

        // Her sektör için panelde gösterilecek çalışma verisini tek yerde toplar.
        public SectorRuntimeData(SectorDefinition sector)
        {
            Sector = sector;
        }

        public SectorDefinition Sector { get; }
        public IReadOnlyList<ProjectExecutionDefinition> AvailableProjects => availableProjects;
        public int ActiveProjectCount { get; private set; }

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

            if (!activeProjectCounts.ContainsKey(project))
            {
                activeProjectCounts.Add(project, 0);
            }
        }

        public void ResetProgress()
        {
            ActiveProjectCount = 0;

            var projectCount = availableProjects.Count;
            for (var i = 0; i < projectCount; i++)
            {
                activeProjectCounts[availableProjects[i]] = 0;
            }
        }

        public void RegisterActiveProject(ProjectExecutionDefinition project)
        {
            if (project == null)
            {
                return;
            }

            AddProject(project);
            ActiveProjectCount++;
            activeProjectCounts[project] = activeProjectCounts[project] + 1;
        }

        public int GetActiveCount(ProjectExecutionDefinition project)
        {
            if (project == null)
            {
                return 0;
            }

            return activeProjectCounts.TryGetValue(project, out var activeCount) ? activeCount : 0;
        }
    }
}
