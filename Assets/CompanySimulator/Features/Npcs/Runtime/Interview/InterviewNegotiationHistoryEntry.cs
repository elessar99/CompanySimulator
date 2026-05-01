using CompanySimulator.Shared.Runtime.Economy;

namespace CompanySimulator.Features.Npcs.Runtime.Interview
{
    public readonly struct InterviewNegotiationHistoryEntry
    {
        public InterviewNegotiationHistoryEntry(
            InterviewNegotiationTurn speaker,
            InterviewDialogueIntent intent,
            Money amount,
            InterviewNegotiationState previousState,
            InterviewNegotiationState nextState)
        {
            Speaker = speaker;
            Intent = intent;
            Amount = amount;
            PreviousState = previousState;
            NextState = nextState;
        }

        public InterviewNegotiationTurn Speaker { get; }
        public InterviewDialogueIntent Intent { get; }
        public Money Amount { get; }
        public InterviewNegotiationState PreviousState { get; }
        public InterviewNegotiationState NextState { get; }
    }
}
