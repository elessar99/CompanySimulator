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
            QualityUpgradeSourceTier = QualityTier;
            PendingQualityUpgradeTier = QualityTier;
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
            QualityUpgradeSourceTier = QualityTier;
            PendingQualityUpgradeTier = QualityTier;
            ApplicantRemainingDays = applicantRemainingDays > 0 ? applicantRemainingDays : 0;
        }

        public string Id { get; }
        public EmployeeProfileDefinition SourceDefinition { get; }
        public string DisplayName { get; }
        public EmployeeRoleDefinition Role { get; }
        public float Quality { get; private set; }
        public Money ExpectedDailySalary { get; }
        public Money AgreedDailySalary { get; private set; }
        public EmployeeQualityTier QualityTier { get; private set; }
        public float IncomeMultiplier { get; private set; }
        public int ApplicantRemainingDays { get; private set; }
        public int EmploymentDays { get; private set; }
        public int QualityProgressDays { get; private set; }
        public bool HasPendingQualityUpgrade { get; private set; }
        public bool IsQualityUpgradeNegotiationActive { get; private set; }
        public EmployeeQualityTier QualityUpgradeSourceTier { get; private set; }
        public EmployeeQualityTier PendingQualityUpgradeTier { get; private set; }
        public int QualityUpgradeRequestRemainingDays { get; private set; }
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
            EmploymentDays = 0;
            QualityProgressDays = 0;
            HasPendingQualityUpgrade = false;
            IsQualityUpgradeNegotiationActive = false;
            QualityUpgradeSourceTier = QualityTier;
            PendingQualityUpgradeTier = QualityTier;
            QualityUpgradeRequestRemainingDays = 0;
        }

        public void SetAgreedDailySalary(Money agreedDailySalary)
        {
            AgreedDailySalary = agreedDailySalary.Amount > 0 ? agreedDailySalary : ExpectedDailySalary;
        }

        public void RestoreRuntimeState(
            float quality,
            Money agreedDailySalary,
            EmployeeQualityTier qualityTier,
            int applicantRemainingDays,
            int employmentDays,
            int qualityProgressDays,
            bool hasPendingQualityUpgrade,
            bool isQualityUpgradeNegotiationActive,
            EmployeeQualityTier qualityUpgradeSourceTier,
            EmployeeQualityTier pendingQualityUpgradeTier,
            int qualityUpgradeRequestRemainingDays,
            string currentAssignmentName)
        {
            Quality = quality;
            QualityTier = qualityTier;
            IncomeMultiplier = Role != null ? Role.GetIncomeMultiplier(QualityTier) : ResolveIncomeMultiplier(QualityTier);
            AgreedDailySalary = agreedDailySalary.Amount > 0 ? agreedDailySalary : Money.Zero;
            ApplicantRemainingDays = applicantRemainingDays > 0 ? applicantRemainingDays : 0;
            EmploymentDays = employmentDays > 0 ? employmentDays : 0;
            QualityProgressDays = qualityProgressDays > 0 ? qualityProgressDays : 0;
            HasPendingQualityUpgrade = hasPendingQualityUpgrade;
            IsQualityUpgradeNegotiationActive = isQualityUpgradeNegotiationActive;
            QualityUpgradeSourceTier = qualityUpgradeSourceTier;
            PendingQualityUpgradeTier = pendingQualityUpgradeTier;
            QualityUpgradeRequestRemainingDays = qualityUpgradeRequestRemainingDays > 0 ? qualityUpgradeRequestRemainingDays : 0;
            CurrentAssignmentName = currentAssignmentName ?? string.Empty;
        }

        public void AdvanceEmploymentDay()
        {
            EmploymentDays++;
        }

        public void AdvanceQualityProgressDay()
        {
            QualityProgressDays++;
        }

        public void StartQualityUpgradeRequest(EmployeeQualityTier nextTier, int responseDays)
        {
            if (HasPendingQualityUpgrade || IsQualityUpgradeNegotiationActive || nextTier <= QualityTier)
            {
                return;
            }

            HasPendingQualityUpgrade = true;
            QualityUpgradeSourceTier = QualityTier;
            PendingQualityUpgradeTier = nextTier;
            QualityUpgradeRequestRemainingDays = responseDays > 0 ? responseDays : 1;
        }

        public bool AdvanceQualityUpgradeRequestDay()
        {
            if (!HasPendingQualityUpgrade || IsQualityUpgradeNegotiationActive)
            {
                return true;
            }

            QualityUpgradeRequestRemainingDays--;
            return QualityUpgradeRequestRemainingDays > 0;
        }

        public void BeginQualityUpgradeNegotiation()
        {
            if (!HasPendingQualityUpgrade || IsQualityUpgradeNegotiationActive)
            {
                return;
            }

            ApplyQualityTier(PendingQualityUpgradeTier);
            IsQualityUpgradeNegotiationActive = true;
            HasPendingQualityUpgrade = false;
            QualityUpgradeRequestRemainingDays = 0;
        }

        public void CompleteQualityUpgradeNegotiation(Money agreedDailySalary)
        {
            SetAgreedDailySalary(agreedDailySalary);
            IsQualityUpgradeNegotiationActive = false;
            QualityProgressDays = 0;
            QualityUpgradeSourceTier = QualityTier;
            PendingQualityUpgradeTier = QualityTier;
        }

        public Money GetQualityUpgradeRequestedSalary()
        {
            var targetTier = IsQualityUpgradeNegotiationActive ? QualityTier : PendingQualityUpgradeTier;
            if (Role == null)
            {
                return EffectiveDailySalary;
            }

            var minimumForTier = Money.From(Role.GetMinimumExpectedSalary(targetTier));
            return minimumForTier > EffectiveDailySalary ? minimumForTier : EffectiveDailySalary + Money.From(1);
        }

        private void ApplyQualityTier(EmployeeQualityTier qualityTier)
        {
            QualityTier = qualityTier;
            Quality = ResolveMinimumQualityForTier(qualityTier);
            IncomeMultiplier = Role != null ? Role.GetIncomeMultiplier(QualityTier) : ResolveIncomeMultiplier(QualityTier);
        }

        private static float ResolveMinimumQualityForTier(EmployeeQualityTier qualityTier)
        {
            switch (qualityTier)
            {
                case EmployeeQualityTier.Kotu:
                    return 10f;
                case EmployeeQualityTier.Ortalama:
                    return 25f;
                case EmployeeQualityTier.Iyi:
                    return 50f;
                default:
                    return 75f;
            }
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
