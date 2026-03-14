using System;
using System.Collections.Generic;
using CompanySimulator.Features.Finance.Runtime.Models;
using CompanySimulator.Shared.Runtime.Economy;

namespace CompanySimulator.Features.Finance.Runtime.Services
{
    public sealed class CompanyLedger
    {
        private readonly List<LedgerEntry> entries = new List<LedgerEntry>(32);
        private Money balance = Money.Zero;

        public Money Balance => balance;
        public IReadOnlyList<LedgerEntry> Entries => entries;

        public void RecordIncome(Money amount, LedgerEntryType type, string description)
        {
            if (amount.IsNegative)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            balance += amount;
            entries.Add(new LedgerEntry(type, amount, description));
        }

        public bool TryRecordExpense(Money amount, LedgerEntryType type, string description)
        {
            if (amount.IsNegative)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            if (balance < amount)
            {
                return false;
            }

            balance -= amount;
            entries.Add(new LedgerEntry(type, -amount, description));
            return true;
        }

        public void Clear()
        {
            balance = Money.Zero;
            entries.Clear();
        }
    }
}
