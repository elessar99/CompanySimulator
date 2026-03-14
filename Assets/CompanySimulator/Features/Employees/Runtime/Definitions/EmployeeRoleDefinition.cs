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

        public Money BaseDailySalary => Money.From(baseDailySalary);
        public float QualityWeight => Mathf.Max(0f, qualityWeight);
        public float ProfitWeight => Mathf.Max(0f, profitWeight);
        public bool RequiresOffice => requiresOffice;
    }
}
