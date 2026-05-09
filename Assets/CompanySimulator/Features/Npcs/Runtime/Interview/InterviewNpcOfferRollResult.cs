using CompanySimulator.Shared.Runtime.Economy;

namespace CompanySimulator.Features.Npcs.Runtime.Interview
{
    public enum InterviewNpcOfferRollType
    {
        None = 0,
        OpeningOffer = 1,
        CounterOffer = 2
    }

    public readonly struct InterviewNpcOfferRollResult
    {
        public InterviewNpcOfferRollResult(InterviewNpcOfferRollType rollType, Money offer, Money minimumOffer, Money maximumOffer, bool canOffer = true)
        {
            RollType = rollType;
            Offer = offer;
            MinimumOffer = minimumOffer;
            MaximumOffer = maximumOffer;
            CanOffer = canOffer;
        }

        public InterviewNpcOfferRollType RollType { get; }
        public Money Offer { get; }
        public Money MinimumOffer { get; }
        public Money MaximumOffer { get; }
        public bool CanOffer { get; }
        public bool HasRange => RollType != InterviewNpcOfferRollType.None;
    }
}
