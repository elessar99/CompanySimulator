using CompanySimulator.Features.Finance.Runtime.Definitions;
using System.Collections.Generic;
using CompanySimulator.Features.Employees.Runtime.Models;
using CompanySimulator.Features.Investments.Runtime.Definitions;
using CompanySimulator.Features.Projects.Runtime.Definitions;
using CompanySimulator.Features.Sectors.Runtime.Definitions;
using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;

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
        private ProjectEconomyResult currentResult;
        private EmployeeRuntimeData[] assignedEmployees;
        private string[] assignedEmployeeNames;
        private InvestmentAllocationInput[] currentInvestmentAllocations;

        public ActiveProjectRuntimeEntry(
            ProjectExecutionDefinition sourceDefinition,
            string displayName,
            ProjectTypeDefinition projectType,
            ProjectEconomyResult startupResult,
            int startedDay,
            EmployeeRuntimeData[] assignedEmployees = null,
            string[] assignedEmployeeNames = null,
            InvestmentAllocationInput[] currentInvestmentAllocations = null,
            float marketDemandMultiplier = 1f,
            float competitorPressure = 0f)
        {
            SourceDefinition = sourceDefinition;
            DisplayName = displayName;
            ProjectType = projectType;
            currentResult = startupResult;
            StartedDay = startedDay;
            this.assignedEmployees = assignedEmployees ?? System.Array.Empty<EmployeeRuntimeData>();
            this.assignedEmployeeNames = assignedEmployeeNames ?? System.Array.Empty<string>();
            this.currentInvestmentAllocations = currentInvestmentAllocations ?? System.Array.Empty<InvestmentAllocationInput>();
            MarketDemandMultiplier = Mathf.Max(0f, marketDemandMultiplier);
            CompetitorPressure = Mathf.Max(0f, competitorPressure);

            var sector = projectType != null ? projectType.Sector : null;
            PayoutIntervalDays = sector != null ? sector.ProfitPayoutIntervalDays : 1;
            NextPayoutDay = startedDay + PayoutIntervalDays;
        }

        public ProjectExecutionDefinition SourceDefinition { get; }
        public string DisplayName { get; }
        public ProjectTypeDefinition ProjectType { get; }
        public ProjectEconomyResult CurrentResult => currentResult;
        public int StartedDay { get; }
        public int PayoutIntervalDays { get; }
        public int NextPayoutDay { get; private set; }
        public int PayoutCount { get; private set; }
        public IReadOnlyList<EmployeeRuntimeData> AssignedEmployees => assignedEmployees;
        public IReadOnlyList<string> AssignedEmployeeNames => assignedEmployeeNames;
        public IReadOnlyList<InvestmentAllocationInput> CurrentInvestmentAllocations => currentInvestmentAllocations;
        public float MarketDemandMultiplier { get; }
        public float CompetitorPressure { get; }
        public SectorDefinition Sector => ProjectType != null ? ProjectType.Sector : null;
        public Money CyclePayrollCost => currentResult.PayrollCost;
        public Money CycleRecurringInvestmentCost => currentResult.RecurringInvestmentCost;
        public Money CycleRevenue => currentResult.Revenue;
        public Money CycleProfit => CycleRevenue - CyclePayrollCost - CycleRecurringInvestmentCost;
        public int DaysUntilNextPayout(int currentDay) => Mathf.Max(0, NextPayoutDay - currentDay);

        public void RegisterPayout()
        {
            PayoutCount++;
            NextPayoutDay += PayoutIntervalDays;
        }

        public void UpdateConfiguration(ProjectEconomyResult result, EmployeeRuntimeData[] employees, string[] employeeNames, InvestmentAllocationInput[] investmentAllocations, int currentDay)
        {
            currentResult = result;
            assignedEmployees = employees ?? System.Array.Empty<EmployeeRuntimeData>();
            assignedEmployeeNames = employeeNames ?? System.Array.Empty<string>();
            currentInvestmentAllocations = investmentAllocations ?? System.Array.Empty<InvestmentAllocationInput>();
            PayoutCount = 0;
            NextPayoutDay = currentDay + PayoutIntervalDays;
        }

        public int GetCurrentBudgetFor(InvestmentTypeDefinition investmentType)
        {
            if (investmentType == null)
            {
                return 0;
            }

            for (var i = 0; i < currentInvestmentAllocations.Length; i++)
            {
                if (currentInvestmentAllocations[i].InvestmentType == investmentType)
                {
                    return currentInvestmentAllocations[i].AllocatedBudgetAmount;
                }
            }

            return 0;
        }
    }
}
