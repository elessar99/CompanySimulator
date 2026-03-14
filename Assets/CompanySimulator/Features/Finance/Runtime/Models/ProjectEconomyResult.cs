using CompanySimulator.Shared.Runtime.Economy;

namespace CompanySimulator.Features.Finance.Runtime.Models
{
    public readonly struct ProjectEconomyResult
    {
        public ProjectEconomyResult(
            int durationDays,
            Money revenue,
            Money payrollCost,
            Money investmentCost,
            Money fixedCost,
            float successScore,
            float employeeContribution,
            float investmentContribution,
            float competitionMultiplier)
        {
            DurationDays = durationDays;
            Revenue = revenue;
            PayrollCost = payrollCost;
            InvestmentCost = investmentCost;
            FixedCost = fixedCost;
            SuccessScore = successScore;
            EmployeeContribution = employeeContribution;
            InvestmentContribution = investmentContribution;
            CompetitionMultiplier = competitionMultiplier;
        }

        public int DurationDays { get; }
        public Money Revenue { get; }
        public Money PayrollCost { get; }
        public Money InvestmentCost { get; }
        public Money FixedCost { get; }
        public Money TotalCosts => PayrollCost + InvestmentCost + FixedCost;
        public Money Profit => Revenue - TotalCosts;
        public float SuccessScore { get; }
        public float EmployeeContribution { get; }
        public float InvestmentContribution { get; }
        public float CompetitionMultiplier { get; }
    }
}
