using CompanySimulator.Features.Employees.Runtime.Models;
using CompanySimulator.Features.Npcs.Runtime.Models;
using CompanySimulator.Shared.Runtime.Economy;

namespace CompanySimulator.Features.Npcs.Runtime.Interview
{
    public sealed class InterviewNpcRuntimeData : NpcRuntimeData
    {
        public InterviewNpcRuntimeData(string runtimeId, EmployeeRuntimeData applicant, Money? expectedDailySalaryOverride = null)
            : base(runtimeId, NpcKind.InterviewCandidate, applicant != null ? applicant.DisplayName : string.Empty)
        {
            Applicant = applicant;
            ExpectedDailySalary = expectedDailySalaryOverride.HasValue
                ? expectedDailySalaryOverride.Value
                : applicant != null ? applicant.ExpectedDailySalary : Money.Zero;
        }

        public EmployeeRuntimeData Applicant { get; }
        public Money ExpectedDailySalary { get; }
    }
}
