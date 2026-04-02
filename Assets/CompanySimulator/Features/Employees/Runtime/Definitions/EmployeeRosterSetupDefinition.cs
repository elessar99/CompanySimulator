using System;
using System.Collections.Generic;
using CompanySimulator.Shared.Runtime.Definitions;
using UnityEngine;

namespace CompanySimulator.Features.Employees.Runtime.Definitions
{
    [CreateAssetMenu(fileName = "EmployeeRosterSetupDefinition", menuName = "Company Simulator/Definitions/Employees/Roster Setup")]
    public sealed class EmployeeRosterSetupDefinition : DefinitionBase
    {
        [SerializeField] private EmployeeRoleDefinition[] availableRoles = Array.Empty<EmployeeRoleDefinition>();
        [SerializeField] private EmployeeProfileDefinition[] startingEmployees = Array.Empty<EmployeeProfileDefinition>();
        [SerializeField] private EmployeeProfileDefinition[] jobApplicants = Array.Empty<EmployeeProfileDefinition>();
        [SerializeField] private bool autoGenerateApplicants = true;
        [SerializeField, Min(1)] private int applicantLifetimeDays = 7;
        [SerializeField, Min(0)] private int initialGeneratedApplicantsPerAllowedSector = 1;
        [SerializeField, Min(0)] private int minGeneratedApplicantsPerRole = 2;
        [SerializeField, Min(0)] private int maxGeneratedApplicantsPerRole = 4;

        public IReadOnlyList<EmployeeRoleDefinition> AvailableRoles => availableRoles;
        public IReadOnlyList<EmployeeProfileDefinition> StartingEmployees => startingEmployees;
        public IReadOnlyList<EmployeeProfileDefinition> JobApplicants => jobApplicants;
        public bool AutoGenerateApplicants => autoGenerateApplicants;
        public int ApplicantLifetimeDays => Mathf.Max(1, applicantLifetimeDays);
        public int InitialGeneratedApplicantsPerAllowedSector => Mathf.Max(0, initialGeneratedApplicantsPerAllowedSector);
        public int MinGeneratedApplicantsPerRole => Mathf.Max(0, minGeneratedApplicantsPerRole);
        public int MaxGeneratedApplicantsPerRole => Mathf.Max(MinGeneratedApplicantsPerRole, maxGeneratedApplicantsPerRole);
    }
}
