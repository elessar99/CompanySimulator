using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;

namespace CompanySimulator.Features.Npcs.Runtime.Interview
{
    public sealed class InterviewCounterOfferService
    {
        public Money CreateCounterOffer(Money npcLastOffer, Money highestPlayerOffer, Money baseExpectation, InterviewNegotiationSettings settings)
        {
            var low = Mathf.Min((float)npcLastOffer.Amount, (float)highestPlayerOffer.Amount);
            var high = Mathf.Max((float)npcLastOffer.Amount, (float)highestPlayerOffer.Amount);
            var raw = Random.Range(low, high <= low ? low + 1f : high);

            var minAllowed = baseExpectation.Amount * settings.CounterOfferMinMultiplier;
            var maxAllowed = baseExpectation.Amount * settings.CounterOfferMaxMultiplier;
            var clamped = Mathf.Clamp(raw, minAllowed, maxAllowed);
            return Money.From(Mathf.RoundToInt(clamped));
        }
    }
}
