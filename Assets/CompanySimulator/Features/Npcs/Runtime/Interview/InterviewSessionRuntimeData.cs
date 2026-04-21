using CompanySimulator.Features.Employees.Runtime.Models;
using CompanySimulator.Features.Furniture.Runtime.Components;
using CompanySimulator.Features.Npcs.Runtime.Models;
using CompanySimulator.Shared.Runtime.Economy;

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
            CurrentSalaryOffer = applicant != null ? applicant.ExpectedDailySalary : Money.Zero;
            State = InterviewSessionState.WaitingForCandidateSpawn;
        }

        public string SessionId { get; }
        public EmployeeRuntimeData Applicant { get; }
        public CeoDeskController Desk { get; }
        public InterviewNpcRuntimeData InterviewNpc { get; }
        public int StartDay { get; }
        public Money CurrentSalaryOffer { get; private set; }
        public InterviewSessionState State { get; private set; }

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

        public void MarkHired(Money agreedSalary)
        {
            CurrentSalaryOffer = agreedSalary;
            State = InterviewSessionState.Hired;
            InterviewNpc?.SetLifecycleState(NpcLifecycleState.Leaving);
        }

        public void MarkRejected()
        {
            State = InterviewSessionState.Rejected;
            InterviewNpc?.SetLifecycleState(NpcLifecycleState.Leaving);
        }

        public void Cancel()
        {
            State = InterviewSessionState.Cancelled;
            InterviewNpc?.SetLifecycleState(NpcLifecycleState.Dismissed);
        }
    }
}
