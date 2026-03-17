using System;
using CompanySimulator.Features.Employees.Runtime.Definitions;
using UnityEngine;

namespace CompanySimulator.Features.Finance.Runtime.Models
{
    [Serializable]
    public struct EmployeeAssignmentInput
    {
        [SerializeField] private EmployeeRoleDefinition role;
        [SerializeField, Min(0)] private int count;
        [SerializeField, Range(0f, 100f)] private float averageQuality;
        [SerializeField, Min(0f)] private float contributionMultiplier;

        public EmployeeAssignmentInput(EmployeeRoleDefinition role, int count, float averageQuality)
            : this(role, count, averageQuality, 1f)
        {
        }

        public EmployeeAssignmentInput(EmployeeRoleDefinition role, int count, float averageQuality, float contributionMultiplier)
        {
            this.role = role;
            this.count = count;
            this.averageQuality = averageQuality;
            this.contributionMultiplier = contributionMultiplier;
        }

        public EmployeeRoleDefinition Role => role;
        public int Count => Mathf.Max(0, count);
        public float AverageQuality => Mathf.Max(0f, averageQuality);
        public float ContributionMultiplier => contributionMultiplier > 0f ? contributionMultiplier : 1f;
    }
}
