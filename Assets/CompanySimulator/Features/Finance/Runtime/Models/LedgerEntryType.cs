namespace CompanySimulator.Features.Finance.Runtime.Models
{
    public enum LedgerEntryType
    {
        Undefined = 0,
        InitialCapital = 1,
        ProjectRevenue = 2,
        PayrollExpense = 3,
        InvestmentExpense = 4,
        RentExpense = 5,
        TaxExpense = 6,
        MiscIncome = 7,
        MiscExpense = 8,
        LoanIncome = 9,
        LoanRepaymentExpense = 10,
        ProjectSaleIncome = 11
    }
}
