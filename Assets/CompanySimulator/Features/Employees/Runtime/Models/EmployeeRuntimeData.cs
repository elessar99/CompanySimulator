using CompanySimulator.Features.Employees.Runtime.Definitions;
using CompanySimulator.Shared.Runtime.Economy;

namespace CompanySimulator.Features.Employees.Runtime.Models
{
    public sealed class EmployeeRuntimeData
    {
        public EmployeeRuntimeData(string id, EmployeeProfileDefinition sourceDefinition)
        {
            Id = id;
            SourceDefinition = sourceDefinition;
            DisplayName = sourceDefinition != null ? sourceDefinition.DisplayName : string.Empty;
            Role = sourceDefinition != null ? sourceDefinition.Role : null;
            Quality = sourceDefinition != null ? sourceDefinition.Quality : 0f;
            ExpectedDailySalary = sourceDefinition != null ? sourceDefinition.ExpectedDailySalary : Money.Zero;
            QualityTier = sourceDefinition != null ? sourceDefinition.QualityTier : EmployeeQualityTier.Kotu;
            IncomeMultiplier = sourceDefinition != null ? sourceDefinition.IncomeMultiplier : 0.5f;
        }

        public EmployeeRuntimeData(string id, string displayName, EmployeeRoleDefinition role, float quality, Money expectedDailySalary)
        {
            Id = id;
            SourceDefinition = null;
            DisplayName = displayName ?? string.Empty;
            Role = role;
            Quality = quality;
            ExpectedDailySalary = expectedDailySalary;
            QualityTier = ResolveQualityTier(quality);
            IncomeMultiplier = ResolveIncomeMultiplier(QualityTier);
        }

        public string Id { get; }
        public EmployeeProfileDefinition SourceDefinition { get; }
        public string DisplayName { get; }
        public EmployeeRoleDefinition Role { get; }
        public float Quality { get; }
        public Money ExpectedDailySalary { get; }
        public EmployeeQualityTier QualityTier { get; }
        public float IncomeMultiplier { get; }
        public string CurrentAssignmentName { get; private set; }
        public bool IsAssigned => !string.IsNullOrWhiteSpace(CurrentAssignmentName);

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
