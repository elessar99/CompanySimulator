using System;
using System.Collections.Generic;
using CompanySimulator.Features.Projects.Runtime.Definitions;
using UnityEngine;

namespace CompanySimulator.Features.Finance.Runtime.Models
{
    public sealed class ProjectEconomyRequest
    {
        public ProjectEconomyRequest(
            ProjectTypeDefinition projectType,
            IReadOnlyList<EmployeeAssignmentInput> employeeAssignments = null,
            IReadOnlyList<InvestmentAllocationInput> investmentAllocations = null,
            float marketDemandMultiplier = 1f,
            float competitorPressure = 0f)
        {
            ProjectType = projectType;
            EmployeeAssignments = employeeAssignments ?? Array.Empty<EmployeeAssignmentInput>();
            InvestmentAllocations = investmentAllocations ?? Array.Empty<InvestmentAllocationInput>();
            MarketDemandMultiplier = Mathf.Max(0f, marketDemandMultiplier);
            CompetitorPressure = Mathf.Max(0f, competitorPressure);
        }

        public ProjectTypeDefinition ProjectType { get; }
        public IReadOnlyList<EmployeeAssignmentInput> EmployeeAssignments { get; }
        public IReadOnlyList<InvestmentAllocationInput> InvestmentAllocations { get; }
        public float MarketDemandMultiplier { get; }
        public float CompetitorPressure { get; }
    }
}
