using CompanySimulator.Features.Employees.Runtime.Definitions;
using CompanySimulator.Shared.Runtime.Economy;

namespace CompanySimulator.Features.Employees.Runtime.Models
{
    public sealed class EmployeeRuntimeData
    {
        public EmployeeRuntimeData(string id, EmployeeProfileDefinition sourceDefinition, int applicantRemainingDays = 0)
        {
            Id = id;
            SourceDefinition = sourceDefinition;
            DisplayName = sourceDefinition != null ? sourceDefinition.DisplayName : string.Empty;
            Role = sourceDefinition != null ? sourceDefinition.Role : null;
            Quality = sourceDefinition != null ? sourceDefinition.Quality : 0f;
            ExpectedDailySalary = sourceDefinition != null ? sourceDefinition.ExpectedDailySalary : Money.Zero;
            QualityTier = sourceDefinition != null ? sourceDefinition.QualityTier : EmployeeQualityTier.Kotu;
            IncomeMultiplier = sourceDefinition != null ? sourceDefinition.IncomeMultiplier : 0.5f;
            ApplicantRemainingDays = applicantRemainingDays > 0 ? applicantRemainingDays : 0;
        }

        public EmployeeRuntimeData(string id, string displayName, EmployeeRoleDefinition role, float quality, Money expectedDailySalary, int applicantRemainingDays = 0)
        {
            Id = id;
            SourceDefinition = null;
            DisplayName = displayName ?? string.Empty;
            Role = role;
            Quality = quality;
            ExpectedDailySalary = expectedDailySalary;
            QualityTier = role != null ? role.GetQualityTier(quality) : ResolveQualityTier(quality);
            IncomeMultiplier = role != null ? role.GetIncomeMultiplier(QualityTier) : ResolveIncomeMultiplier(QualityTier);
            ApplicantRemainingDays = applicantRemainingDays > 0 ? applicantRemainingDays : 0;
        }

        public string Id { get; }
        public EmployeeProfileDefinition SourceDefinition { get; }
        public string DisplayName { get; }
        public EmployeeRoleDefinition Role { get; }
        public float Quality { get; }
        public Money ExpectedDailySalary { get; }
        public Money AgreedDailySalary { get; private set; }
        public EmployeeQualityTier QualityTier { get; }
        public float IncomeMultiplier { get; }
        public int ApplicantRemainingDays { get; private set; }
        public string CurrentAssignmentName { get; private set; }
        public bool IsAssigned => !string.IsNullOrWhiteSpace(CurrentAssignmentName);
        public Money EffectiveDailySalary => AgreedDailySalary.Amount > 0 ? AgreedDailySalary : ExpectedDailySalary;

        public bool TryAssign(string assignmentName)
        {
            if (IsAssigned || string.IsNullOrWhiteSpace(assignmentName))
            {
                return false;
            }

            CurrentAssignmentName = assignmentName;
            return true;
        }

        public void ClearAssignment()
        {
            CurrentAssignmentName = string.Empty;
        }

        public void SetApplicantRemainingDays(int remainingDays)
        {
            ApplicantRemainingDays = remainingDays > 0 ? remainingDays : 0;
        }

        public bool AdvanceApplicantDay()
        {
            if (ApplicantRemainingDays <= 0)
            {
                return false;
            }

            ApplicantRemainingDays--;
            return ApplicantRemainingDays > 0;
        }

        public void MarkAsEmployee()
        {
            ApplicantRemainingDays = 0;
        }

        public void SetAgreedDailySalary(Money agreedDailySalary)
        {
            AgreedDailySalary = agreedDailySalary.Amount > 0 ? agreedDailySalary : ExpectedDailySalary;
        }

        private static EmployeeQualityTier ResolveQualityTier(float quality)
        {
            if (quality < 25f)
            {
                return EmployeeQualityTier.Kotu;
            }

            if (quality < 50f)
            {
                return EmployeeQualityTier.Ortalama;
            }

            if (quality < 75f)
            {
                return EmployeeQualityTier.Iyi;
            }

            return EmployeeQualityTier.Profesyonel;
        }

        private static float ResolveIncomeMultiplier(EmployeeQualityTier qualityTier)
        {
            switch (qualityTier)
            {
                case EmployeeQualityTier.Kotu:
                    return 0.5f;
                case EmployeeQualityTier.Ortalama:
                    return 1f;
                case EmployeeQualityTier.Iyi:
                    return 1.5f;
                default:
                    return 3f;
            }
        }
    }
}
