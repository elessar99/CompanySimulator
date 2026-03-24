using System.Collections.Generic;
using CompanySimulator.Shared.Runtime.Economy;

namespace CompanySimulator.Features.FinanceOverview.Runtime.Models
{
    public readonly struct FinanceDaySnapshot
    {
        public FinanceDaySnapshot(int day, IReadOnlyList<FinanceLineItemSnapshot> incomes, IReadOnlyList<FinanceLineItemSnapshot> expenses, Money totalIncome, Money totalExpense)
        {
            Day = day;
            Incomes = incomes;
            Expenses = expenses;
            TotalIncome = totalIncome;
            TotalExpense = totalExpense;
        }

        public int Day { get; }
        public IReadOnlyList<FinanceLineItemSnapshot> Incomes { get; }
        public IReadOnlyList<FinanceLineItemSnapshot> Expenses { get; }
        public Money TotalIncome { get; }
        public Money TotalExpense { get; }
        public Money NetAmount => TotalIncome - TotalExpense;
    }
}
