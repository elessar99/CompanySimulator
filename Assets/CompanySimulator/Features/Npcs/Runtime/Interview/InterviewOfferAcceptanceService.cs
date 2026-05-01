using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;

namespace CompanySimulator.Features.Npcs.Runtime.Interview
{
    public sealed class InterviewOfferAcceptanceService
    {
        public float CalculateOfferRatio(Money playerOffer, Money baseExpectation)
        {
            if (baseExpectation.Amount <= 0)
            {
                return 1f;
            }

            return (float)playerOffer.Amount / baseExpectation.Amount;
        }

        public float CalculateAcceptanceProbability(Money playerOffer, Money baseExpectation, InterviewNegotiationSettings settings)
        {
            if (baseExpectation.Amount <= 0)
            {
                return 1f;
            }

            var ratio = CalculateOfferRatio(playerOffer, baseExpectation);
            if (ratio <= settings.LowOfferHardRejectionMultiplier)
            {
                return 0f;
            }

            if (ratio >= settings.GuaranteedAcceptanceMultiplier)
            {
                return 1f;
            }

            var min = settings.LowOfferHardRejectionMultiplier;
            var max = settings.GuaranteedAcceptanceMultiplier;
            var t = Mathf.Clamp01((ratio - min) / Mathf.Max(0.0001f, max - min));
            return t * t;
        }

        public bool ShouldAccept(Money playerOffer, Money baseExpectation, InterviewNegotiationSettings settings)
        {
            var probability = CalculateAcceptanceProbability(playerOffer, baseExpectation, settings);
            return Random.value <= probability;
        }

        public bool TryEvaluateOffer(Money playerOffer, Money baseExpectation, InterviewNegotiationSettings settings, out float ratio, out float probability, out float roll)
        {
            ratio = CalculateOfferRatio(playerOffer, baseExpectation);
            probability = CalculateAcceptanceProbability(playerOffer, baseExpectation, settings);
            roll = Random.value;
            return roll <= probability;
        }
    }
}
