using System;
using System.Collections.Generic;
using CompanySimulator.Features.Rivals.Runtime.Definitions;
using CompanySimulator.Features.Save.Runtime.Models;
using CompanySimulator.Features.Save.Runtime.Services;
using CompanySimulator.Features.Sectors.Runtime.Definitions;
using CompanySimulator.Features.Sectors.Runtime.Services;
using CompanySimulator.Shared.Runtime.Economy;

namespace CompanySimulator.Features.Rivals.Runtime.Models
{
    public sealed class RivalCompanyRuntimeData
    {
        private readonly List<RivalCompanyJobRuntimeData> activeJobs = new List<RivalCompanyJobRuntimeData>(8);
        private readonly List<SectorDefinition> cachedSectors = new List<SectorDefinition>(4);
        private readonly List<RivalJobLogEntry> jobStartLog = new List<RivalJobLogEntry>(8);
        private readonly List<RivalJobLogEntry> jobSellLog = new List<RivalJobLogEntry>(8);
        private Money balance;
        private int daysSinceLastJobCheck;
        private int daysSinceLastSellCheck;
        private int daysSinceLastAgentCheck;

        public RivalCompanyRuntimeData(RivalCompanyDefinition definition)
        {
            Definition = definition;
            balance = Money.From(definition.StartingBalance);
            daysSinceLastJobCheck = 0;
            daysSinceLastSellCheck = 0;
            daysSinceLastAgentCheck = 0;
            RebuildSectorCache();
        }

        public RivalCompanyDefinition Definition { get; }
        public Money Balance => balance;
        public Money CompanyValue => CalculateCompanyValue();
        public IReadOnlyList<RivalCompanyJobRuntimeData> ActiveJobs => activeJobs;
        public IReadOnlyList<SectorDefinition> OperatingSectors => cachedSectors;
        public int ActiveJobCount => activeJobs.Count;
        public int DaysSinceLastJobCheck => daysSinceLastJobCheck;
        public int DaysSinceLastSellCheck => daysSinceLastSellCheck;
        public int DaysUntilNextJobCheck => Math.Max(0, Definition.JobCheckIntervalDays - daysSinceLastJobCheck);
        public int DaysUntilNextSellCheck => Math.Max(0, Definition.SellCheckIntervalDays - daysSinceLastSellCheck);
        public int DaysSinceLastAgentCheck => daysSinceLastAgentCheck;
        public int DaysUntilNextAgentCheck => Math.Max(0, Definition.AgentSendCheckIntervalDays - daysSinceLastAgentCheck);
        public IReadOnlyList<RivalJobLogEntry> JobStartLog => jobStartLog;
        public IReadOnlyList<RivalJobLogEntry> JobSellLog => jobSellLog;

        public bool AdvanceDay(int currentDay)
        {
            ProcessJobPayouts();
            daysSinceLastJobCheck++;
            daysSinceLastSellCheck++;
            daysSinceLastAgentCheck++;

            if (daysSinceLastSellCheck >= Definition.SellCheckIntervalDays)
            {
                daysSinceLastSellCheck = 0;
                TrySellJobs(currentDay);
            }

            if (daysSinceLastJobCheck >= Definition.JobCheckIntervalDays)
            {
                daysSinceLastJobCheck = 0;
                TryStartJobs(currentDay);
            }

            var shouldSendAgent = false;
            if (daysSinceLastAgentCheck >= Definition.AgentSendCheckIntervalDays)
            {
                daysSinceLastAgentCheck = 0;
                shouldSendAgent = true;
            }

            return shouldSendAgent;
        }

        public RivalCompanySaveData CaptureSaveData()
        {
            var saveData = new RivalCompanySaveData
            {
                definitionId = Definition != null ? Definition.Id : string.Empty,
                balance = balance.Amount,
                daysSinceLastJobCheck = daysSinceLastJobCheck,
                daysSinceLastSellCheck = daysSinceLastSellCheck,
                daysSinceLastAgentCheck = daysSinceLastAgentCheck
            };

            for (var i = 0; i < activeJobs.Count; i++)
            {
                var job = activeJobs[i];
                if (job == null || job.Definition == null)
                {
                    continue;
                }

                saveData.activeJobs.Add(new RivalCompanyJobSaveData
                {
                    definitionId = job.Definition.Id,
                    startDay = job.StartDay,
                    daysSinceLastPayout = job.DaysSinceLastPayout,
                    lastEarnedIncome = job.LastEarnedIncome.Amount,
                    isAgentAffected = job.IsAgentAffected,
                    agentRevenueReductionMultiplier = job.AgentRevenueReductionMultiplier
                });
            }

            CaptureLog(jobStartLog, saveData.jobStartLog);
            CaptureLog(jobSellLog, saveData.jobSellLog);
            return saveData;
        }

        public bool RestoreFromSaveData(RivalCompanySaveData saveData, GameSaveDefinitionResolver resolver, out string validationMessage)
        {
            validationMessage = string.Empty;
            if (saveData == null)
            {
                validationMessage = "Rakip firma kayıt verisi bulunamadı.";
                return false;
            }

            for (var i = 0; i < saveData.activeJobs.Count; i++)
            {
                var jobId = saveData.activeJobs[i].definitionId;
                if (!resolver.TryResolve<RivalCompanyJobDefinition>(jobId, out _))
                {
                    validationMessage = $"Rakip iş tanımı bulunamadı: {jobId}";
                    return false;
                }
            }

            activeJobs.Clear();
            jobStartLog.Clear();
            jobSellLog.Clear();
            balance = Money.From(saveData.balance);
            daysSinceLastJobCheck = Math.Max(0, saveData.daysSinceLastJobCheck);
            daysSinceLastSellCheck = Math.Max(0, saveData.daysSinceLastSellCheck);
            daysSinceLastAgentCheck = Math.Max(0, saveData.daysSinceLastAgentCheck);

            for (var i = 0; i < saveData.activeJobs.Count; i++)
            {
                var savedJob = saveData.activeJobs[i];
                resolver.TryResolve<RivalCompanyJobDefinition>(savedJob.definitionId, out var jobDefinition);
                var runtimeJob = new RivalCompanyJobRuntimeData(jobDefinition, savedJob.startDay);
                runtimeJob.RestoreState(
                    savedJob.daysSinceLastPayout,
                    Money.From(savedJob.lastEarnedIncome),
                    savedJob.isAgentAffected,
                    savedJob.agentRevenueReductionMultiplier);
                activeJobs.Add(runtimeJob);
            }

            RestoreLog(saveData.jobStartLog, resolver, jobStartLog);
            RestoreLog(saveData.jobSellLog, resolver, jobSellLog);
            RebuildSectorCache();
            return true;
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
                jobStartLog.Add(new RivalJobLogEntry(selected.DisplayName, selected.Sector, currentDay, Money.From(selected.JobCost)));
                started++;
            }

            if (started > 0)
            {
                RebuildSectorCache();
            }
        }

        private void TrySellJobs(int currentDay)
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
                var sellPayout = Money.From(adjustedAvgIncome * job.Definition.AbandonRevenueMultiplier);
                balance = balance + sellPayout;

                var sector = job.Sector;
                if (sector != null && sector.CompetitionLingerDays > 0)
                {
                    SectorCompetitionService.RegisterLingeringProject(sector, sector.CompetitionLingerDays);
                }

                jobSellLog.Add(new RivalJobLogEntry(job.Definition.DisplayName, sector, currentDay, sellPayout));
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

        private static void CaptureLog(IReadOnlyList<RivalJobLogEntry> source, List<RivalJobLogSaveData> target)
        {
            for (var i = 0; i < source.Count; i++)
            {
                var entry = source[i];
                target.Add(new RivalJobLogSaveData
                {
                    jobName = entry.JobName,
                    sectorId = entry.Sector != null ? entry.Sector.Id : string.Empty,
                    day = entry.Day,
                    amount = entry.Amount.Amount
                });
            }
        }

        private static void RestoreLog(IReadOnlyList<RivalJobLogSaveData> source, GameSaveDefinitionResolver resolver, List<RivalJobLogEntry> target)
        {
            if (source == null)
            {
                return;
            }

            for (var i = 0; i < source.Count; i++)
            {
                var savedLog = source[i];
                resolver.TryResolve<SectorDefinition>(savedLog.sectorId, out var sector);
                target.Add(new RivalJobLogEntry(savedLog.jobName, sector, savedLog.day, Money.From(savedLog.amount)));
            }
        }
    }
}
