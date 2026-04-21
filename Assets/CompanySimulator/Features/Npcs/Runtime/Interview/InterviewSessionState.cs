namespace CompanySimulator.Features.Npcs.Runtime.Interview
{
    public enum InterviewSessionState
    {
        None = 0,
        WaitingForCandidateSpawn = 1,
        CandidateSeated = 2,
        NegotiationReady = 3,
        Hired = 4,
        Rejected = 5,
        Cancelled = 6
    }
}
