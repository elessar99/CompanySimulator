using System;
using System.Collections.Generic;
using CompanySimulator.Features.Employees.Runtime.Definitions;
using CompanySimulator.Features.Investments.Runtime.Definitions;
using CompanySimulator.Features.Sectors.Runtime.Definitions;
using CompanySimulator.Shared.Runtime.Definitions;
using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;

namespace CompanySimulator.Features.Projects.Runtime.Definitions
{
    [CreateAssetMenu(fileName = "ProjectTypeDefinition", menuName = "Company Simulator/Definitions/Projects/Project Type")]
    public sealed class ProjectTypeDefinition : DefinitionBase
    {
        [SerializeField] private SectorDefinition sector;
        [SerializeField, Min(0)] private int baseRevenue = 100000;
        [SerializeField, Min(0)] private int fixedCost = 10000;
        [SerializeField, Min(1)] private int baseDurationDays = 14;
        [SerializeField, Min(0.1f)] private float baseSuccessScore = 1f;
        [SerializeField, Min(0.1f)] private float demandMultiplier = 1f;
        [SerializeField] private EmployeeRoleDefinition[] preferredRoles = Array.Empty<EmployeeRoleDefinition>();
        [SerializeField] private InvestmentTypeDefinition[] recommendedInvestments = Array.Empty<InvestmentTypeDefinition>();

        public SectorDefinition Sector => sector;
        public Money BaseRevenue => Money.From(baseRevenue);
        public Money FixedCost => Money.From(fixedCost);
        public int BaseDurationDays => Mathf.Max(1, baseDurationDays);
        public float BaseSuccessScore => Mathf.Max(0.1f, baseSuccessScore);
        public float DemandMultiplier => Mathf.Max(0.1f, demandMultiplier);
        public IReadOnlyList<EmployeeRoleDefinition> PreferredRoles => preferredRoles;
        public IReadOnlyList<InvestmentTypeDefinition> RecommendedInvestments => recommendedInvestments;
    }
}
