namespace CompanySimulator.Features.Npcs.Runtime.Interview
{
    public enum InterviewNegotiationState
    {
        NotStarted = 0,
        NpcOpeningStatement = 1,
        WaitingForPlayerDecisionOnNpcOffer = 2,
        WaitingForPlayerOpeningOffer = 3,
        EvaluatingPlayerOffer = 4,
        NpcCounterOffer = 5,
        WaitingForPlayerDecisionOnCounterOffer = 6,
        Accepted = 7,
        RejectedByNpc = 8,
        RejectedByPlayer = 9,
        InterviewClosed = 10
    }
}
