using UnityEngine;

namespace CompanySimulator.Features.Npcs.Runtime.Interview
{
    [System.Serializable]
    public struct InterviewNegotiationSettings
    {
        [SerializeField, Range(0f, 1f)] private float npcOpensWithOfferProbability;
        [SerializeField, Min(0.1f)] private float npcOpeningOfferMinMultiplier;
        [SerializeField, Min(0.1f)] private float npcOpeningOfferMaxMultiplier;
        [SerializeField, Min(0.1f)] private float lowOfferHardRejectionMultiplier;
        [SerializeField, Min(0.1f)] private float guaranteedAcceptanceMultiplier;
        [SerializeField, Range(0f, 1f)] private float rejectThenEndProbability;
        [SerializeField, Range(0f, 1f)] private float rejectThenCounterProbability;
        [SerializeField, Min(0.1f)] private float counterOfferMinMultiplier;
        [SerializeField, Min(0.1f)] private float counterOfferMaxMultiplier;

        public float NpcOpensWithOfferProbability => Mathf.Clamp01(npcOpensWithOfferProbability);
        public float NpcOpeningOfferMinMultiplier => Mathf.Max(0.1f, npcOpeningOfferMinMultiplier);
        public float NpcOpeningOfferMaxMultiplier => Mathf.Max(NpcOpeningOfferMinMultiplier, npcOpeningOfferMaxMultiplier);
        public float LowOfferHardRejectionMultiplier => Mathf.Max(0.1f, lowOfferHardRejectionMultiplier);
        public float GuaranteedAcceptanceMultiplier => Mathf.Max(LowOfferHardRejectionMultiplier, guaranteedAcceptanceMultiplier);
        public float RejectThenEndProbability => Mathf.Clamp01(rejectThenEndProbability);
        public float RejectThenCounterProbability => Mathf.Clamp01(rejectThenCounterProbability);
        public float CounterOfferMinMultiplier => Mathf.Max(0.1f, counterOfferMinMultiplier);
        public float CounterOfferMaxMultiplier => Mathf.Max(CounterOfferMinMultiplier, counterOfferMaxMultiplier);

        public static InterviewNegotiationSettings Default => new InterviewNegotiationSettings
        {
            npcOpensWithOfferProbability = 0.55f,
            npcOpeningOfferMinMultiplier = 0.9f,
            npcOpeningOfferMaxMultiplier = 1.5f,
            lowOfferHardRejectionMultiplier = 0.5f,
            guaranteedAcceptanceMultiplier = 1f,
            rejectThenEndProbability = 0.45f,
            rejectThenCounterProbability = 0.55f,
            counterOfferMinMultiplier = 0.7f,
            counterOfferMaxMultiplier = 1.2f
        };
    }
}
