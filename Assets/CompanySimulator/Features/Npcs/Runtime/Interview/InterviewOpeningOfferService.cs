using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;

namespace CompanySimulator.Features.Npcs.Runtime.Interview
{
    public sealed class InterviewOpeningOfferService
    {
        public bool ShouldNpcOpenWithOffer(InterviewNegotiationSettings settings)
        {
            return Random.value <= settings.NpcOpensWithOfferProbability;
        }

        public Money CreateNpcOpeningOffer(Money baseExpectation, InterviewNegotiationSettings settings)
        {
            var expectation = Mathf.Max(1f, baseExpectation.Amount);
            var multiplier = Random.Range(settings.NpcOpeningOfferMinMultiplier, settings.NpcOpeningOfferMaxMultiplier);
            return Money.From(Mathf.RoundToInt(expectation * multiplier));
        }
    }
}
