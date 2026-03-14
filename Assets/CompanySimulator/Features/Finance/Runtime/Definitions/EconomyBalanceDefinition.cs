using CompanySimulator.Shared.Runtime.Definitions;
using UnityEngine;

namespace CompanySimulator.Features.Finance.Runtime.Definitions
{
    [CreateAssetMenu(fileName = "EconomyBalanceDefinition", menuName = "Company Simulator/Definitions/Finance/Economy Balance")]
    public sealed class EconomyBalanceDefinition : DefinitionBase
    {
        [SerializeField, Min(1f)] private float qualityNormalizationPoint = 100f;
        [SerializeField, Range(0f, 2f)] private float employeeProfitImpact = 0.35f;
        [SerializeField, Range(0f, 2f)] private float employeeSuccessImpact = 0.5f;
        [SerializeField, Range(0f, 2f)] private float investmentProfitImpact = 0.35f;
        [SerializeField, Range(0f, 2f)] private float investmentSuccessImpact = 0.5f;
        [SerializeField, Range(0f, 2f)] private float competitionImpact = 1f;
        [SerializeField, Min(0.05f)] private float minimumCompetitionMultiplier = 0.25f;
        [SerializeField, Min(0.05f)] private float minimumSuccessScore = 0.1f;

        public float QualityNormalizationPoint => Mathf.Max(1f, qualityNormalizationPoint);
        public float EmployeeProfitImpact => Mathf.Max(0f, employeeProfitImpact);
        public float EmployeeSuccessImpact => Mathf.Max(0f, employeeSuccessImpact);
        public float InvestmentProfitImpact => Mathf.Max(0f, investmentProfitImpact);
        public float InvestmentSuccessImpact => Mathf.Max(0f, investmentSuccessImpact);
        public float CompetitionImpact => Mathf.Max(0f, competitionImpact);
        public float MinimumCompetitionMultiplier => Mathf.Max(0.05f, minimumCompetitionMultiplier);
        public float MinimumSuccessScore => Mathf.Max(0.05f, minimumSuccessScore);
    }
}
