using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;

namespace CompanySimulator.Features.Npcs.Runtime.Interview
{
    public sealed class InterviewDialogueGenerator
    {
        private int lastVariant = -1;

        public InterviewDialoguePayload CreatePayload(InterviewDialogueIntent intent, Money amount)
        {
            var variantCount = GetVariantCount(intent);
            var suffix = Random.Range(0, variantCount);
            if (suffix == lastVariant)
            {
                suffix = (suffix + 1) % variantCount;
            }

            lastVariant = suffix;
            var lineKey = intent.ToString() + "." + suffix;
            return new InterviewDialoguePayload(intent, lineKey, amount);
        }

        private static int GetVariantCount(InterviewDialogueIntent intent)
        {
            switch (intent)
            {
                case InterviewDialogueIntent.NpcOpeningOffer:
                case InterviewDialogueIntent.NpcRequestsPlayerOffer:
                case InterviewDialogueIntent.NpcCounterOffers:
                case InterviewDialogueIntent.NpcAcceptsOffer:
                case InterviewDialogueIntent.NpcHardRejectsOffer:
                case InterviewDialogueIntent.NpcSoftRejectsOffer:
                    return 4;
                default:
                    return 2;
            }
        }
    }
}
