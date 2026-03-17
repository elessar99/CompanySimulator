using CompanySimulator.Shared.Runtime.Definitions;
using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;

namespace CompanySimulator.Features.Employees.Runtime.Definitions
{
    public enum EmployeeQualityTier
    {
        Kotu = 0,
        Ortalama = 1,
        Iyi = 2,
        Profesyonel = 3
    }

    [CreateAssetMenu(fileName = "EmployeeProfileDefinition", menuName = "Company Simulator/Definitions/Employees/Profile")]
    public sealed class EmployeeProfileDefinition : DefinitionBase
    {
        [SerializeField] private EmployeeRoleDefinition role;
        [SerializeField, Range(0f, 100f)] private float quality = 50f;
        [SerializeField, Min(0)] private int expectedDailySalary = 100;

        public EmployeeRoleDefinition Role => role;
        public float Quality => Mathf.Clamp(quality, 0f, 100f);
        public Money ExpectedDailySalary => Money.From(expectedDailySalary);

        public EmployeeQualityTier QualityTier
        {
            get
            {
                return role != null ? role.GetQualityTier(Quality) : ResolveFallbackQualityTier(Quality);
            }
        }

        public float IncomeMultiplier
        {
            get
            {
                return role != null ? role.GetIncomeMultiplier(QualityTier) : 1f;
            }
        }

        private static EmployeeQualityTier ResolveFallbackQualityTier(float quality)
        {
            if (quality < 25f)
            {
                return EmployeeQualityTier.Kotu;
            }

            if (quality < 50f)
            {
                return EmployeeQualityTier.Ortalama;
            }

            if (quality < 75f)
            {
                return EmployeeQualityTier.Iyi;
            }

            return EmployeeQualityTier.Profesyonel;
        }
    }
}
