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
                if (Quality < 25f)
                {
                    return EmployeeQualityTier.Kotu;
                }

                if (Quality < 50f)
                {
                    return EmployeeQualityTier.Ortalama;
                }

                if (Quality < 75f)
                {
                    return EmployeeQualityTier.Iyi;
                }

                return EmployeeQualityTier.Profesyonel;
            }
        }

        public float IncomeMultiplier
        {
            get
            {
                switch (QualityTier)
                {
                    case EmployeeQualityTier.Kotu:
                        return 0.5f;
                    case EmployeeQualityTier.Ortalama:
                        return 1f;
                    case EmployeeQualityTier.Iyi:
                        return 1.5f;
                    default:
                        return 3f;
                }
            }
        }
    }
}
