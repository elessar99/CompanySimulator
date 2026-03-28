using System;
using System.Collections.Generic;
using CompanySimulator.Features.Rivals.Runtime.Definitions;
using CompanySimulator.Features.Sectors.Runtime.Definitions;
using CompanySimulator.Features.Sectors.Runtime.Services;
using CompanySimulator.Shared.Runtime.Economy;

namespace CompanySimulator.Features.Rivals.Runtime.Models
{
    public sealed class RivalCompanyRuntimeData
    {
        private readonly List<RivalCompanyJobRuntimeData> activeJobs = new List<RivalCompanyJobRuntimeData>(8);
        private readonly List<SectorDefinition> cachedSectors = new List<SectorDefinition>(4);
        private Money balance;
        private int daysSinceLastJobCheck;
        private int daysSinceLastSellCheck;

        public RivalCompanyRuntimeData(RivalCompanyDefinition definition)
        {
            Definition = definition;
            balance = Money.From(definition.StartingBalance);
            daysSinceLastJobCheck = 0;
            daysSinceLastSellCheck = 0;
            RebuildSectorCache();
        }

        public RivalCompanyDefinition Definition { get; }
        public Money Balance => balance;
        public Money CompanyValue => CalculateCompanyValue();
        public IReadOnlyList<RivalCompanyJobRuntimeData> ActiveJobs => activeJobs;
        public IReadOnlyList<SectorDefinition> OperatingSectors => cachedSectors;
        public int ActiveJobCount => activeJobs.Count;

        public void AdvanceDay(int currentDay)
        {
            ProcessJobPayouts();
            TryAbandonJobs();
            daysSinceLastJobCheck++;
            daysSinceLastSellCheck++;

            if (daysSinceLastSellCheck >= Definition.SellCheckIntervalDays)
            {
                daysSinceLastSellCheck = 0;
                TrySellJobs();
            }

            if (daysSinceLastJobCheck >= Definition.JobCheckIntervalDays)
            {
                daysSinceLastJobCheck = 0;
                TryStartJobs(currentDay);
            }
        }

        private void ProcessJobPayouts()
        {
            for (var i = 0; i < activeJobs.Count; i++)
            {
                var job = activeJobs[i];
                var multiplier = SectorCompetitionService.GetCachedRevenueMultiplier(job.Sector);
                var income = job.AdvanceDay(multiplier);
                if (income > Money.Zero)
                {
                    balance = balance + income;
                }
            }
        }

        private void TryStartJobs(int currentDay)
        {
            var jobs = Definition.AvailableJobs;
            if (jobs.Count == 0)
            {
                return;
            }

            var started = 0;
            var maxPerCheck = Definition.MaxJobsPerCheck;

            for (var attempt = 0; attempt < maxPerCheck; attempt++)
            {
                var roll = UnityEngine.Random.value;
                if (roll > Definition.JobStartChance)
                {
                    continue;
                }

                var selected = SelectAffordableJobByWeight(jobs);
                if (selected == null)
                {
                    break;
                }

                balance = balance - Money.From(selected.JobCost);
                activeJobs.Add(new RivalCompanyJobRuntimeData(selected, currentDay));
                started++;
            }

            if (started > 0)
            {
                RebuildSectorCache();
            }
        }

        private void TryAbandonJobs()
        {
            var abandoned = false;
            for (var i = activeJobs.Count - 1; i >= 0; i--)
            {
                var job = activeJobs[i];
                var multiplier = SectorCompetitionService.GetCachedRevenueMultiplier(job.Sector);
                var safeMultiplier = multiplier > 0f ? multiplier : 0.01f;

                var effectiveAbandonChance = job.Definition.AbandonChance / safeMultiplier / safeMultiplier;
                var roll = UnityEngine.Random.value;
                if (roll > effectiveAbandonChance)
                {
                    continue;
                }

                var avgIncome = (job.Definition.MinimumIncomePerCycle + job.Definition.MaximumIncomePerCycle) / 2;
                var adjustedAvgIncome = avgIncome * multiplier;
                var abandonPayout = Money.From(adjustedAvgIncome * job.Definition.AbandonRevenueMultiplier);
                balance = balance + abandonPayout;

                var sector = job.Sector;
                if (sector != null && sector.CompetitionLingerDays > 0)
                {
                    SectorCompetitionService.RegisterLingeringProject(sector, sector.CompetitionLingerDays);
                }

                activeJobs.RemoveAt(i);
                abandoned = true;
            }

            if (abandoned)
            {
                RebuildSectorCache();
            }
        }

        private void TrySellJobs()
        {
            if (activeJobs.Count == 0 || Definition.SellDesireMultiplier <= 0f)
            {
                return;
            }

            var sold = 0;
            var maxSells = Definition.MaxSellsPerCheck;

            for (var i = activeJobs.Count - 1; i >= 0 && sold < maxSells; i--)
            {
                var job = activeJobs[i];
                var multiplier = SectorCompetitionService.GetCachedRevenueMultiplier(job.Sector);
                var safeMultiplier = multiplier > 0f ? multiplier : 0.01f;

                var baseSellChance = job.Definition.AbandonChance * Definition.SellDesireMultiplier;
                var effectiveSellChance = baseSellChance / safeMultiplier / safeMultiplier;

                var roll = UnityEngine.Random.value;
                if (roll > effectiveSellChance)
                {
                    continue;
                }

                var avgIncome = (job.Definition.MinimumIncomePerCycle + job.Definition.MaximumIncomePerCycle) / 2;
                var adjustedAvgIncome = avgIncome * multiplier;
                var sector = job.Sector;
                var saleMultiplier = sector != null ? sector.SaleRevenueMultiplier : 1f;
                var sellPayout = Money.From(adjustedAvgIncome * saleMultiplier);
                balance = balance + sellPayout;

                if (sector != null && sector.CompetitionLingerDays > 0)
                {
                    SectorCompetitionService.RegisterLingeringProject(sector, sector.CompetitionLingerDays);
                }

                activeJobs.RemoveAt(i);
                sold++;
            }

            if (sold > 0)
            {
                RebuildSectorCache();
            }
        }

        private RivalCompanyJobDefinition SelectAffordableJobByWeight(IReadOnlyList<RivalCompanyJobDefinition> jobs)
        {
            var totalWeight = 0;
            for (var i = 0; i < jobs.Count; i++)
            {
                var job = jobs[i];
                if (job.Sector == null) continue;
                if (balance < Money.From(job.JobCost)) continue;
                var multiplier = SectorCompetitionService.GetCachedRevenueMultiplier(job.Sector);
                var effectiveWeight = (int)System.Math.Ceiling(job.SelectionWeight * multiplier * multiplier);
                if (effectiveWeight < 1) effectiveWeight = 1;
                totalWeight += effectiveWeight;
            }

            if (totalWeight <= 0)
            {
                return null;
            }

            var pick = UnityEngine.Random.Range(0, totalWeight);
            var cumulative = 0;
            for (var i = 0; i < jobs.Count; i++)
            {
                var job = jobs[i];
                if (job.Sector == null) continue;
                if (balance < Money.From(job.JobCost)) continue;
                var multiplier = SectorCompetitionService.GetCachedRevenueMultiplier(job.Sector);
                var effectiveWeight = (int)System.Math.Ceiling(job.SelectionWeight * multiplier * multiplier);
                if (effectiveWeight < 1) effectiveWeight = 1;
                cumulative += effectiveWeight;
                if (pick < cumulative)
                {
                    return job;
                }
            }

            return null;
        }

        private void RebuildSectorCache()
        {
            cachedSectors.Clear();

            for (var i = 0; i < Definition.AvailableJobs.Count; i++)
            {
                var sector = Definition.AvailableJobs[i].Sector;
                if (sector != null && !cachedSectors.Contains(sector))
                {
                    cachedSectors.Add(sector);
                }
            }
        }

        private Money CalculateCompanyValue()
        {
            var value = balance;
            for (var i = 0; i < activeJobs.Count; i++)
            {
                var job = activeJobs[i];
                var avgIncomePerCycle = (job.Definition.MinimumIncomePerCycle + job.Definition.MaximumIncomePerCycle) / 2;
                var multiplier = SectorCompetitionService.GetCachedRevenueMultiplier(job.Sector);
                value = value + Money.From(avgIncomePerCycle * multiplier);
            }

            return value;
        }
    }
}
