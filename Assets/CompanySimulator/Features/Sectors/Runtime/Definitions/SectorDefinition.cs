using CompanySimulator.Shared.Runtime.Definitions;
using UnityEngine;

namespace CompanySimulator.Features.Sectors.Runtime.Definitions
{
    [CreateAssetMenu(fileName = "SectorDefinition", menuName = "Company Simulator/Definitions/Sectors/Sector")]
    public sealed class SectorDefinition : DefinitionBase
    {
        [SerializeField, Min(0.1f)] private float revenueMultiplier = 1f;
        [SerializeField, Min(0.1f)] private float durationMultiplier = 1f;
        [SerializeField, Range(0f, 2f)] private float competitionSensitivity = 1f;
        [SerializeField, Min(0f)] private float successToRevenueWeight = 0.5f;

        public float RevenueMultiplier => Mathf.Max(0.1f, revenueMultiplier);
        public float DurationMultiplier => Mathf.Max(0.1f, durationMultiplier);
        public float CompetitionSensitivity => Mathf.Max(0f, competitionSensitivity);
        public float SuccessToRevenueWeight => Mathf.Max(0f, successToRevenueWeight);
    }
}
