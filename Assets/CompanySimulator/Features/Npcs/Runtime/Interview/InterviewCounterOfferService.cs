using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;

namespace CompanySimulator.Features.Npcs.Runtime.Interview
{
    public sealed class InterviewCounterOfferService
    {
        public InterviewNpcOfferRollResult CreateCounterOffer(Money npcLastOffer, Money highestPlayerOffer, Money baseExpectation, InterviewNegotiationSettings settings)
        {
            var expectation = Mathf.Max(1f, baseExpectation.Amount);
            var configuredMin = Mathf.RoundToInt(expectation * settings.CounterOfferMinMultiplier);
            var configuredMax = Mathf.RoundToInt(expectation * settings.CounterOfferMaxMultiplier);
            var playerFloor = highestPlayerOffer.Amount + 1;
            var effectiveMin = System.Math.Max((long)configuredMin, playerFloor);
            var previousNpcCap = npcLastOffer.Amount > 0 ? npcLastOffer.Amount : configuredMax;
            var effectiveMax = System.Math.Min(previousNpcCap, configuredMax);

            if (effectiveMin > effectiveMax)
            {
                return new InterviewNpcOfferRollResult(
                    InterviewNpcOfferRollType.CounterOffer,
                    Money.Zero,
                    Money.From(effectiveMin),
                    Money.From(effectiveMax),
                    false);
            }

            var rolledAmount = effectiveMin >= effectiveMax
                ? effectiveMin
                : Random.Range((int)effectiveMin, (int)effectiveMax + 1);
            var offer = Money.From(rolledAmount);
            return new InterviewNpcOfferRollResult(
                InterviewNpcOfferRollType.CounterOffer,
                offer,
                Money.From(effectiveMin),
                Money.From(effectiveMax));
        }
    }
}
