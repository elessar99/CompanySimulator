using CompanySimulator.Features.Finance.Runtime.Definitions;
using CompanySimulator.Features.Finance.Runtime.Models;
using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;

namespace CompanySimulator.Features.Finance.Runtime.Services
{
    public sealed class ProjectEconomyCalculator
    {
        private readonly EconomyBalanceDefinition balanceDefinition;

        public ProjectEconomyCalculator(EconomyBalanceDefinition balanceDefinition)
        {
            this.balanceDefinition = balanceDefinition;
        }

        public ProjectEconomyResult Calculate(ProjectEconomyRequest request)
        {
            if (balanceDefinition == null)
            {
                throw new System.InvalidOperationException("EconomyBalanceDefinition is required.");
            }

            if (request == null)
            {
                throw new System.ArgumentNullException(nameof(request));
            }

            if (request.ProjectType == null)
            {
                throw new System.ArgumentException("ProjectType is required.", nameof(request));
            }

            if (request.ProjectType.Sector == null)
            {
                throw new System.ArgumentException("ProjectType must reference a sector.", nameof(request));
            }

            var projectType = request.ProjectType;
            var sector = projectType.Sector;
            var durationDays = Mathf.Max(1, Mathf.CeilToInt(projectType.BaseDurationDays * sector.DurationMultiplier));

            var payrollCost = CalculatePayrollCost(request, durationDays, out var employeeProfitContribution, out var employeeSuccessContribution);
            CalculateInvestmentCosts(
                request,
                out var upfrontInvestmentCost,
                out var recurringInvestmentCost,
                out var investmentProfitContribution,
                out var investmentSuccessContribution);
            var fixedCost = projectType.FixedCost;
            var competitionMultiplier = CalculateCompetitionMultiplier(request.CompetitorPressure, sector.CompetitionSensitivity);

            var successScore = projectType.BaseSuccessScore;
            successScore *= 1f + employeeSuccessContribution * balanceDefinition.EmployeeSuccessImpact;
            successScore *= 1f + investmentSuccessContribution * balanceDefinition.InvestmentSuccessImpact;
            successScore = Mathf.Max(balanceDefinition.MinimumSuccessScore, successScore);

            var profitMultiplier = 1f;
            profitMultiplier += employeeProfitContribution * balanceDefinition.EmployeeProfitImpact;
            profitMultiplier += investmentProfitContribution * balanceDefinition.InvestmentProfitImpact;
            profitMultiplier = Mathf.Max(0f, profitMultiplier);

            var successRevenueMultiplier = 1f + Mathf.Max(0f, successScore - 1f) * sector.SuccessToRevenueWeight;
            var revenueAmount = projectType.BaseRevenue.Amount;
            var grossRevenue = revenueAmount * sector.RevenueMultiplier * projectType.DemandMultiplier * request.MarketDemandMultiplier * competitionMultiplier * successRevenueMultiplier * profitMultiplier;
            var revenue = Money.From(grossRevenue);

            return new ProjectEconomyResult(
                durationDays,
                revenue,
                payrollCost,
                upfrontInvestmentCost,
                recurringInvestmentCost,
                fixedCost,
                successScore,
                employeeProfitContribution,
                investmentProfitContribution,
                competitionMultiplier);
        }

        private Money CalculatePayrollCost(ProjectEconomyRequest request, int durationDays, out float profitContribution, out float successContribution)
        {
            var assignments = request.EmployeeAssignments;
            var totalPayrollCost = Money.Zero;
            var totalEmployeeCount = 0;
            var totalProfitWeight = 0f;
            var totalSuccessWeight = 0f;

            for (var i = 0; i < assignments.Count; i++)
            {
                var assignment = assignments[i];
                var role = assignment.Role;
                var count = assignment.Count;

                if (role == null || count <= 0)
                {
                    continue;
                }

                totalEmployeeCount += count;
                totalPayrollCost += role.BaseDailySalary * count * durationDays;

                var normalizedQuality = assignment.AverageQuality / balanceDefinition.QualityNormalizationPoint;
                var contributionMultiplier = assignment.ContributionMultiplier;
                totalProfitWeight += normalizedQuality * role.ProfitWeight * contributionMultiplier * count;
                totalSuccessWeight += normalizedQuality * role.QualityWeight * contributionMultiplier * count;
            }

            if (totalEmployeeCount <= 0)
            {
                profitContribution = 0f;
                successContribution = 0f;
                return totalPayrollCost;
            }

            profitContribution = totalProfitWeight / totalEmployeeCount;
            successContribution = totalSuccessWeight / totalEmployeeCount;
            return totalPayrollCost;
        }

        private void CalculateInvestmentCosts(
            ProjectEconomyRequest request,
            out Money upfrontInvestmentCost,
            out Money recurringInvestmentCost,
            out float profitContribution,
            out float successContribution)
        {
            var allocations = request.InvestmentAllocations;
            upfrontInvestmentCost = Money.Zero;
            recurringInvestmentCost = Money.Zero;
            var weightedProfitValue = 0f;
            var weightedSuccessValue = 0f;
            var totalProfitWeight = 0f;
            var totalSuccessWeight = 0f;

            for (var i = 0; i < allocations.Count; i++)
            {
                var allocation = allocations[i];
                var investmentType = allocation.InvestmentType;
                var allocatedBudgetAmount = allocation.AllocatedBudgetAmount;

                if (investmentType == null || allocatedBudgetAmount <= 0)
                {
                    continue;
                }

                if (investmentType.IsRecurringExpense)
                {
                    recurringInvestmentCost += allocation.AllocatedBudget;
                }
                else
                {
                    upfrontInvestmentCost += allocation.AllocatedBudget;
                }

                var budgetMultiplier = investmentType.EvaluateBudgetMultiplier(allocatedBudgetAmount);
                weightedProfitValue += budgetMultiplier * investmentType.ProfitWeight;
                weightedSuccessValue += budgetMultiplier * investmentType.SuccessWeight;
                totalProfitWeight += investmentType.ProfitWeight;
                totalSuccessWeight += investmentType.SuccessWeight;
            }

            profitContribution = totalProfitWeight > 0f ? (weightedProfitValue / totalProfitWeight) - 1f : 0f;
            successContribution = totalSuccessWeight > 0f ? (weightedSuccessValue / totalSuccessWeight) - 1f : 0f;
        }

        private float CalculateCompetitionMultiplier(float competitorPressure, float sectorSensitivity)
        {
            var rawMultiplier = 1f - (competitorPressure * sectorSensitivity * balanceDefinition.CompetitionImpact);
            return Mathf.Max(balanceDefinition.MinimumCompetitionMultiplier, rawMultiplier);
        }
    }
}
