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

        public InterviewNpcOfferRollResult CreateNpcOpeningOffer(Money baseExpectation, InterviewNegotiationSettings settings, long minimumFloor = 0)
        {
            var expectation = Mathf.Max(1f, baseExpectation.Amount);
            var configuredMinimumOffer = Mathf.RoundToInt(expectation * settings.NpcOpeningOfferMinMultiplier);
            var minimumOffer = System.Math.Max((long)configuredMinimumOffer, minimumFloor);
            var maximumOffer = Mathf.RoundToInt(expectation * settings.NpcOpeningOfferMaxMultiplier);
            if (minimumOffer > maximumOffer)
            {
                return new InterviewNpcOfferRollResult(
                    InterviewNpcOfferRollType.OpeningOffer,
                    Money.Zero,
                    Money.From(minimumOffer),
                    Money.From(maximumOffer),
                    false);
            }

            var rolledAmount = minimumOffer >= maximumOffer
                ? minimumOffer
                : Random.Range((int)minimumOffer, maximumOffer + 1);
            var offer = Money.From(rolledAmount);
            return new InterviewNpcOfferRollResult(
                InterviewNpcOfferRollType.OpeningOffer,
                offer,
                Money.From(minimumOffer),
                Money.From(maximumOffer));
        }
    }
}
