using System;
using System.Collections.Generic;
using CompanySimulator.Features.Sectors.Runtime.Definitions;
using CompanySimulator.Shared.Runtime.Definitions;
using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;

namespace CompanySimulator.Features.Employees.Runtime.Definitions
{
    [CreateAssetMenu(fileName = "EmployeeRoleDefinition", menuName = "Company Simulator/Definitions/Employees/Role")]
    public sealed class EmployeeRoleDefinition : DefinitionBase
    {
        [SerializeField, Min(0)] private int baseDailySalary = 100;
        [SerializeField, Min(0f)] private float qualityWeight = 1f;
        [SerializeField, Min(0f)] private float profitWeight = 1f;
        [SerializeField] private bool requiresOffice = true;
        [SerializeField, Min(1)] private int maxConcurrentAssignmentsPerEmployee = 1;
        [SerializeField] private SectorDefinition[] allowedSectors = Array.Empty<SectorDefinition>();

        public Money BaseDailySalary => Money.From(baseDailySalary);
        public float QualityWeight => Mathf.Max(0f, qualityWeight);
        public float ProfitWeight => Mathf.Max(0f, profitWeight);
        public bool RequiresOffice => requiresOffice;
        public int MaxConcurrentAssignmentsPerEmployee => Mathf.Max(1, maxConcurrentAssignmentsPerEmployee);
        public IReadOnlyList<SectorDefinition> AllowedSectors => allowedSectors;

        public bool CanWorkInSector(SectorDefinition sector)
        {
            if (sector == null)
            {
                return false;
            }

            if (allowedSectors == null || allowedSectors.Length == 0)
            {
                return true;
            }

            for (var i = 0; i < allowedSectors.Length; i++)
            {
                if (allowedSectors[i] == sector)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
