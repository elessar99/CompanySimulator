using System;
using System.Collections.Generic;
using CompanySimulator.Features.Employees.Runtime.Definitions;
using CompanySimulator.Features.Investments.Runtime.Definitions;
using CompanySimulator.Shared.Runtime.Definitions;
using UnityEngine;

namespace CompanySimulator.Features.Sectors.Runtime.Definitions
{
    [CreateAssetMenu(fileName = "SectorDefinition", menuName = "Company Simulator/Definitions/Sectors/Sector")]
    public sealed class SectorDefinition : DefinitionBase
    {
        [SerializeField, TextArea] private string description;
        [SerializeField, Min(0.1f)] private float revenueMultiplier = 1f;
        [SerializeField, Min(0.1f)] private float durationMultiplier = 1f;
        [SerializeField, Range(0f, 2f)] private float competitionSensitivity = 1f;
        [SerializeField, Min(0f)] private float successToRevenueWeight = 0.5f;
        [SerializeField, Min(1)] private int profitPayoutIntervalDays = 7;
        [SerializeField] private SectorRiskLevel riskLevel = SectorRiskLevel.Dusuk;
        [SerializeField, Range(0f, 1f)] private float revenueRiskRatio = 0.1f;
        [SerializeField] private EmployeeRoleDefinition[] supportedRoles = Array.Empty<EmployeeRoleDefinition>();
        [SerializeField] private InvestmentTypeDefinition[] availableInvestments = Array.Empty<InvestmentTypeDefinition>();

        public string Description => description;
        public float RevenueMultiplier => Mathf.Max(0.1f, revenueMultiplier);
        public float DurationMultiplier => Mathf.Max(0.1f, durationMultiplier);
        public float CompetitionSensitivity => Mathf.Max(0f, competitionSensitivity);
        public float SuccessToRevenueWeight => Mathf.Max(0f, successToRevenueWeight);
        public int ProfitPayoutIntervalDays => Mathf.Max(1, profitPayoutIntervalDays);
        public SectorRiskLevel RiskLevel => riskLevel;
        public float RevenueRiskRatio => Mathf.Clamp01(revenueRiskRatio);
        public IReadOnlyList<EmployeeRoleDefinition> SupportedRoles => supportedRoles;
        public IReadOnlyList<InvestmentTypeDefinition> AvailableInvestments => availableInvestments;
    }
}
