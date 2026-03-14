using System;
using System.Collections.Generic;
using CompanySimulator.Features.Finance.Runtime.Models;
using CompanySimulator.Features.Investments.Runtime.Definitions;
using CompanySimulator.Features.Projects.Runtime.Definitions;
using CompanySimulator.Shared.Runtime.Definitions;
using UnityEngine;

namespace CompanySimulator.Features.Finance.Runtime.Definitions
{
    [CreateAssetMenu(fileName = "ProjectExecutionDefinition", menuName = "Company Simulator/Definitions/Finance/Project Execution")]
    public sealed class ProjectExecutionDefinition : DefinitionBase
    {
        [SerializeField] private ProjectTypeDefinition projectType;
        [SerializeField] private EmployeeAssignmentInput[] employeeAssignments = Array.Empty<EmployeeAssignmentInput>();
        [SerializeField] private InvestmentAllocationInput[] investmentAllocations = Array.Empty<InvestmentAllocationInput>();
        [SerializeField, Min(0f)] private float marketDemandMultiplier = 1f;
        [SerializeField, Range(0f, 1f)] private float competitorPressure;

        public ProjectTypeDefinition ProjectType => projectType;
        public IReadOnlyList<EmployeeAssignmentInput> EmployeeAssignments => employeeAssignments;
        public IReadOnlyList<InvestmentAllocationInput> InvestmentAllocations => investmentAllocations;
        public float MarketDemandMultiplier => Mathf.Max(0f, marketDemandMultiplier);
        public float CompetitorPressure => Mathf.Max(0f, competitorPressure);

        public ProjectEconomyRequest CreateRequest()
        {
            return new ProjectEconomyRequest(
                projectType,
                employeeAssignments,
                investmentAllocations,
                MarketDemandMultiplier,
                CompetitorPressure);
        }

        public int GetAllocatedBudgetFor(InvestmentTypeDefinition investmentType)
        {
            if (investmentType == null)
            {
                return 0;
            }

            for (var i = 0; i < investmentAllocations.Length; i++)
            {
                if (investmentAllocations[i].InvestmentType == investmentType)
                {
                    return investmentAllocations[i].AllocatedBudgetAmount;
                }
            }

            return 0;
        }
    }
}
