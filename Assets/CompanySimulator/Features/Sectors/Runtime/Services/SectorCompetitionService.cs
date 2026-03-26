using System.Collections.Generic;
using CompanySimulator.Features.Finance.Runtime.Models;
using CompanySimulator.Features.Rivals.Runtime.Models;
using CompanySimulator.Features.Sectors.Runtime.Definitions;
using UnityEngine;

namespace CompanySimulator.Features.Sectors.Runtime.Services
{
    public static class SectorCompetitionService
    {
        private static readonly Dictionary<SectorDefinition, int> ProjectCountCache = new Dictionary<SectorDefinition, int>(8);

        public static float GetRevenueMultiplier(
            SectorDefinition sector,
            IReadOnlyList<ActiveProjectRuntimeEntry> playerProjects,
            IReadOnlyList<RivalCompanyRuntimeData> rivals)
        {
            if (sector == null || sector.CompetitionRevenueCurve == null)
            {
                return 1f;
            }

            var totalProjects = CountProjectsInSector(sector, playerProjects, rivals);
            return Mathf.Clamp01(sector.CompetitionRevenueCurve.Evaluate(totalProjects));
        }

        public static int CountProjectsInSector(
            SectorDefinition sector,
            IReadOnlyList<ActiveProjectRuntimeEntry> playerProjects,
            IReadOnlyList<RivalCompanyRuntimeData> rivals)
        {
            if (sector == null)
            {
                return 0;
            }

            var count = 0;

            if (playerProjects != null)
            {
                for (var i = 0; i < playerProjects.Count; i++)
                {
                    if (playerProjects[i].Sector == sector)
                    {
                        count++;
                    }
                }
            }

            if (rivals != null)
            {
                for (var i = 0; i < rivals.Count; i++)
                {
                    var rival = rivals[i];
                    var jobs = rival.ActiveJobs;
                    for (var j = 0; j < jobs.Count; j++)
                    {
                        if (jobs[j].Sector == sector)
                        {
                            count++;
                        }
                    }
                }
            }

            return count;
        }

        public static void BuildProjectCountCache(
            IReadOnlyList<ActiveProjectRuntimeEntry> playerProjects,
            IReadOnlyList<RivalCompanyRuntimeData> rivals)
        {
            ProjectCountCache.Clear();

            if (playerProjects != null)
            {
                for (var i = 0; i < playerProjects.Count; i++)
                {
                    var sector = playerProjects[i].Sector;
                    if (sector == null) continue;
                    ProjectCountCache.TryGetValue(sector, out var current);
                    ProjectCountCache[sector] = current + 1;
                }
            }

            if (rivals != null)
            {
                for (var i = 0; i < rivals.Count; i++)
                {
                    var jobs = rivals[i].ActiveJobs;
                    for (var j = 0; j < jobs.Count; j++)
                    {
                        var sector = jobs[j].Sector;
                        if (sector == null) continue;
                        ProjectCountCache.TryGetValue(sector, out var current);
                        ProjectCountCache[sector] = current + 1;
                    }
                }
            }
        }

        public static float GetCachedRevenueMultiplier(SectorDefinition sector)
        {
            if (sector == null || sector.CompetitionRevenueCurve == null)
            {
                return 1f;
            }

            ProjectCountCache.TryGetValue(sector, out var count);
            return Mathf.Clamp01(sector.CompetitionRevenueCurve.Evaluate(count));
        }
    }
}
