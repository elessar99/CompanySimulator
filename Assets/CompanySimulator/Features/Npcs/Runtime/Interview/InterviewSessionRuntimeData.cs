using CompanySimulator.Features.Employees.Runtime.Models;
using CompanySimulator.Features.Furniture.Runtime.Components;
using CompanySimulator.Features.Npcs.Runtime.Models;
using CompanySimulator.Shared.Runtime.Economy;
using System.Collections.Generic;

namespace CompanySimulator.Features.Npcs.Runtime.Interview
{
    public sealed class InterviewSessionRuntimeData
    {
        public InterviewSessionRuntimeData(string sessionId, EmployeeRuntimeData applicant, CeoDeskController desk, InterviewNpcRuntimeData interviewNpc, int startDay)
        {
            SessionId = sessionId;
            Applicant = applicant;
            Desk = desk;
            InterviewNpc = interviewNpc;
            StartDay = startDay;
            BaseExpectation = applicant != null ? applicant.ExpectedDailySalary : Money.Zero;
            CurrentSalaryOffer = BaseExpectation;
            HighestPlayerOffer = Money.Zero;
            LastPlayerOffer = Money.Zero;
            NpcOpeningOffer = Money.Zero;
            NpcLastOffer = Money.Zero;
            CurrentTurn = InterviewNegotiationTurn.System;
            NegotiationState = InterviewNegotiationState.NotStarted;
            NegotiationOutcome = InterviewNegotiationOutcome.None;
            LatestDialogue = new InterviewDialoguePayload(InterviewDialogueIntent.None, string.Empty, Money.Zero);
            DebugReason = string.Empty;
            State = InterviewSessionState.WaitingForCandidateSpawn;
        }

        private readonly List<InterviewNegotiationHistoryEntry> negotiationHistory = new List<InterviewNegotiationHistoryEntry>(16);

        public string SessionId { get; }
        public EmployeeRuntimeData Applicant { get; }
        public CeoDeskController Desk { get; }
        public InterviewNpcRuntimeData InterviewNpc { get; }
        public int StartDay { get; }
        public Money BaseExpectation { get; }
        public Money CurrentSalaryOffer { get; private set; }
        public Money HighestPlayerOffer { get; private set; }
        public Money LastPlayerOffer { get; private set; }
        public Money NpcOpeningOffer { get; private set; }
        public Money NpcLastOffer { get; private set; }
        public InterviewSessionState State { get; private set; }
        public InterviewNegotiationState NegotiationState { get; private set; }
        public InterviewNegotiationTurn CurrentTurn { get; private set; }
        public InterviewNegotiationOutcome NegotiationOutcome { get; private set; }
        public bool WasNpcOpeningOfferBranch { get; private set; }
        public bool IsFinalDecisionStage { get; private set; }
        public InterviewDialoguePayload LatestDialogue { get; private set; }
        public float LastPlayerOfferRatio { get; private set; }
        public float LastAcceptanceProbability { get; private set; }
        public float LastAcceptanceRoll { get; private set; }
        public string DebugReason { get; private set; }
        public IReadOnlyList<InterviewNegotiationHistoryEntry> NegotiationHistory => negotiationHistory;

        public void MarkCandidateSeated()
        {
            State = InterviewSessionState.CandidateSeated;
            InterviewNpc?.SetLifecycleState(NpcLifecycleState.Seated);
        }

        public void MarkNegotiationReady()
        {
            State = InterviewSessionState.NegotiationReady;
            InterviewNpc?.SetLifecycleState(NpcLifecycleState.Interviewing);
        }

        public void UpdateSalaryOffer(Money salaryOffer)
        {
            CurrentSalaryOffer = salaryOffer;
        }

        public void SetNpcOpeningOffer(Money salaryOffer)
        {
            NpcOpeningOffer = salaryOffer;
            NpcLastOffer = salaryOffer;
            CurrentSalaryOffer = salaryOffer;
            WasNpcOpeningOfferBranch = true;
        }

        public void SetNpcLastOffer(Money salaryOffer)
        {
            NpcLastOffer = salaryOffer;
            CurrentSalaryOffer = salaryOffer;
        }

        public void SetPlayerOffer(Money salaryOffer)
        {
            LastPlayerOffer = salaryOffer;
            if (salaryOffer.Amount > HighestPlayerOffer.Amount)
            {
                HighestPlayerOffer = salaryOffer;
            }
        }

        public void SetNegotiationState(InterviewNegotiationState negotiationState)
        {
            NegotiationState = negotiationState;
        }

        public void SetCurrentTurn(InterviewNegotiationTurn turn)
        {
            CurrentTurn = turn;
        }

        public void SetFinalDecisionStage(bool isFinalDecisionStage)
        {
            IsFinalDecisionStage = isFinalDecisionStage;
        }

        public void SetLatestDialogue(InterviewDialoguePayload payload)
        {
            LatestDialogue = payload;
        }

        public void AddHistoryEntry(InterviewNegotiationHistoryEntry entry)
        {
            negotiationHistory.Add(entry);
        }

        public void SetLatestOfferEvaluation(float offerRatio, float acceptanceProbability, float acceptanceRoll, string debugReason)
        {
            LastPlayerOfferRatio = offerRatio;
            LastAcceptanceProbability = acceptanceProbability;
            LastAcceptanceRoll = acceptanceRoll;
            DebugReason = debugReason ?? string.Empty;
        }

        public void SetDebugReason(string debugReason)
        {
            DebugReason = debugReason ?? string.Empty;
        }

        public void MarkHired(Money agreedSalary)
        {
            CurrentSalaryOffer = agreedSalary;
            State = InterviewSessionState.Hired;
            NegotiationOutcome = InterviewNegotiationOutcome.Accepted;
            NegotiationState = InterviewNegotiationState.Accepted;
            InterviewNpc?.SetLifecycleState(NpcLifecycleState.Leaving);
        }

        public void MarkRejected()
        {
            State = InterviewSessionState.Rejected;
            NegotiationOutcome = InterviewNegotiationOutcome.RejectedByNpc;
            NegotiationState = InterviewNegotiationState.RejectedByNpc;
            InterviewNpc?.SetLifecycleState(NpcLifecycleState.Leaving);
        }

        public void MarkRejectedByPlayer()
        {
            State = InterviewSessionState.Rejected;
            NegotiationOutcome = InterviewNegotiationOutcome.RejectedByPlayer;
            NegotiationState = InterviewNegotiationState.RejectedByPlayer;
            InterviewNpc?.SetLifecycleState(NpcLifecycleState.Leaving);
        }

        public void Cancel()
        {
            State = InterviewSessionState.Cancelled;
            NegotiationOutcome = InterviewNegotiationOutcome.Cancelled;
            NegotiationState = InterviewNegotiationState.InterviewClosed;
            InterviewNpc?.SetLifecycleState(NpcLifecycleState.Dismissed);
        }
    }
}
