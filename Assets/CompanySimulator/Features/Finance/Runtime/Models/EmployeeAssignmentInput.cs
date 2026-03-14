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

        public EmployeeAssignmentInput(EmployeeRoleDefinition role, int count, float averageQuality)
        {
            this.role = role;
            this.count = count;
            this.averageQuality = averageQuality;
        }

        public EmployeeRoleDefinition Role => role;
        public int Count => Mathf.Max(0, count);
        public float AverageQuality => Mathf.Max(0f, averageQuality);
    }
}
