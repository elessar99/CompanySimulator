using System;
using System.Collections.Generic;
using CompanySimulator.Features.Sectors.Runtime.Definitions;
using CompanySimulator.Shared.Runtime.Definitions;
using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;

namespace CompanySimulator.Features.Employees.Runtime.Definitions
{
    [Serializable]
    public struct EmployeeQualityTierSettings
    {
        [SerializeField, Min(0)] private int minimumExpectedSalary;
        [SerializeField, Min(0)] private int maximumExpectedSalary;
        [SerializeField, Min(0f)] private float incomeMultiplier;
        [SerializeField, Min(0f)] private float spawnChanceWeight;

        public EmployeeQualityTierSettings(int minimumExpectedSalary, int maximumExpectedSalary, float incomeMultiplier, float spawnChanceWeight)
        {
            this.minimumExpectedSalary = minimumExpectedSalary;
            this.maximumExpectedSalary = maximumExpectedSalary;
            this.incomeMultiplier = incomeMultiplier;
            this.spawnChanceWeight = spawnChanceWeight;
        }

        public int MinimumExpectedSalary => Mathf.Max(0, minimumExpectedSalary);
        public int MaximumExpectedSalary => Mathf.Max(MinimumExpectedSalary, maximumExpectedSalary);
        public float IncomeMultiplier => Mathf.Max(0f, incomeMultiplier);
        public float SpawnChanceWeight => Mathf.Max(0f, spawnChanceWeight);
    }

    [CreateAssetMenu(fileName = "EmployeeRoleDefinition", menuName = "Company Simulator/Definitions/Employees/Role")]
    public sealed class EmployeeRoleDefinition : DefinitionBase
    {
        [SerializeField, Min(0)] private int baseDailySalary = 100;
        [SerializeField, Min(0f)] private float qualityWeight = 1f;
        [SerializeField, Min(0f)] private float profitWeight = 1f;
        [SerializeField] private bool requiresOffice = true;
        [SerializeField, Min(1)] private int applicantRefreshIntervalDays = 3;
        [SerializeField, Min(1)] private int maxConcurrentAssignmentsPerEmployee = 1;
        [SerializeField] private SectorDefinition[] allowedSectors = Array.Empty<SectorDefinition>();
        [SerializeField] private EmployeeQualityTierSettings kotuSettings = new EmployeeQualityTierSettings(300, 450, 0.5f, 35f);
        [SerializeField] private EmployeeQualityTierSettings ortalamaSettings = new EmployeeQualityTierSettings(451, 600, 1f, 35f);
        [SerializeField] private EmployeeQualityTierSettings iyiSettings = new EmployeeQualityTierSettings(601, 725, 1.5f, 20f);
        [SerializeField] private EmployeeQualityTierSettings profesyonelSettings = new EmployeeQualityTierSettings(726, 800, 3f, 10f);
        [Header("Quality Growth")]
        [SerializeField, Min(1)] private int kotuQualityUpgradeDays = 10;
        [SerializeField, Min(1)] private int ortalamaQualityUpgradeDays = 15;
        [SerializeField, Min(1)] private int iyiQualityUpgradeDays = 20;

        public Money BaseDailySalary => Money.From(baseDailySalary);
        public float QualityWeight => Mathf.Max(0f, qualityWeight);
        public float ProfitWeight => Mathf.Max(0f, profitWeight);
        public bool RequiresOffice => requiresOffice;
        public int ApplicantRefreshIntervalDays => Mathf.Max(1, applicantRefreshIntervalDays);

        // Şimdilik her çalışan aynı anda sadece tek bir işte çalışabilir.
        public int MaxConcurrentAssignmentsPerEmployee => 1;
        public IReadOnlyList<SectorDefinition> AllowedSectors => allowedSectors;
        public EmployeeQualityTierSettings KotuSettings => kotuSettings;
        public EmployeeQualityTierSettings OrtalamaSettings => ortalamaSettings;
        public EmployeeQualityTierSettings IyiSettings => iyiSettings;
        public EmployeeQualityTierSettings ProfesyonelSettings => profesyonelSettings;
        public int KotuQualityUpgradeDays => Mathf.Max(1, kotuQualityUpgradeDays);
        public int OrtalamaQualityUpgradeDays => Mathf.Max(1, ortalamaQualityUpgradeDays);
        public int IyiQualityUpgradeDays => Mathf.Max(1, iyiQualityUpgradeDays);

        public EmployeeQualityTier GetQualityTier(float quality)
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

        public int GetMinimumExpectedSalary(EmployeeQualityTier qualityTier)
        {
            return GetSettings(qualityTier).MinimumExpectedSalary;
        }

        public int GetMaximumExpectedSalary(EmployeeQualityTier qualityTier)
        {
            return GetSettings(qualityTier).MaximumExpectedSalary;
        }

        public float GetIncomeMultiplier(EmployeeQualityTier qualityTier)
        {
            return GetSettings(qualityTier).IncomeMultiplier;
        }

        public float GetSpawnChanceWeight(EmployeeQualityTier qualityTier)
        {
            return GetSettings(qualityTier).SpawnChanceWeight;
        }

        public int GetQualityUpgradeDays(EmployeeQualityTier qualityTier)
        {
            switch (qualityTier)
            {
                case EmployeeQualityTier.Kotu:
                    return KotuQualityUpgradeDays;
                case EmployeeQualityTier.Ortalama:
                    return OrtalamaQualityUpgradeDays;
                case EmployeeQualityTier.Iyi:
                    return IyiQualityUpgradeDays;
                default:
                    return 0;
            }
        }

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

        private EmployeeQualityTierSettings GetSettings(EmployeeQualityTier qualityTier)
        {
            switch (qualityTier)
            {
                case EmployeeQualityTier.Kotu:
                    return kotuSettings;
                case EmployeeQualityTier.Ortalama:
                    return ortalamaSettings;
                case EmployeeQualityTier.Iyi:
                    return iyiSettings;
                default:
                    return profesyonelSettings;
            }
        }
    }
}
