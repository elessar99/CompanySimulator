namespace CompanySimulator.Features.Npcs.Runtime.Interview
{
    public enum InterviewDialogueIntent
    {
        None = 0,
        NpcOpeningOffer = 1,
        NpcRequestsPlayerOffer = 2,
        NpcAcceptsOffer = 3,
        NpcSoftRejectsOffer = 4,
        NpcHardRejectsOffer = 5,
        NpcCounterOffers = 6,
        NpcEndsInterview = 7,
        PlayerAcceptedNpcOffer = 8,
        PlayerRejectedNpcOffer = 9
    }
}
