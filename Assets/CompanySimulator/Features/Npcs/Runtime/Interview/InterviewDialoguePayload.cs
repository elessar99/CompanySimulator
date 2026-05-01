using CompanySimulator.Shared.Runtime.Economy;

namespace CompanySimulator.Features.Npcs.Runtime.Interview
{
    public readonly struct InterviewDialoguePayload
    {
        public InterviewDialoguePayload(InterviewDialogueIntent intent, string lineKey, Money amount)
        {
            Intent = intent;
            LineKey = lineKey ?? string.Empty;
            Amount = amount;
        }

        public InterviewDialogueIntent Intent { get; }
        public string LineKey { get; }
        public Money Amount { get; }
    }
}
