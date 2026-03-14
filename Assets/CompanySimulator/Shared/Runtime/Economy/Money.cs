using System;

namespace CompanySimulator.Shared.Runtime.Economy
{
    [Serializable]
    public readonly struct Money : IEquatable<Money>, IComparable<Money>
    {
        public static readonly Money Zero = new Money(0);

        public Money(long amount)
        {
            Amount = amount;
        }

        public long Amount { get; }
        public bool IsNegative => Amount < 0;

        public static Money From(long amount)
        {
            return new Money(amount);
        }

        public static Money From(double amount)
        {
            return new Money(Convert.ToInt64(Math.Round(amount, MidpointRounding.AwayFromZero)));
        }

        public int CompareTo(Money other)
        {
            return Amount.CompareTo(other.Amount);
        }

        public bool Equals(Money other)
        {
            return Amount == other.Amount;
        }

        public override bool Equals(object obj)
        {
            return obj is Money other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Amount.GetHashCode();
        }

        public override string ToString()
        {
            return Amount.ToString();
        }

        public static Money operator +(Money left, Money right)
        {
            return new Money(left.Amount + right.Amount);
        }

        public static Money operator -(Money left, Money right)
        {
            return new Money(left.Amount - right.Amount);
        }

        public static Money operator -(Money value)
        {
            return new Money(-value.Amount);
        }

        public static Money operator *(Money value, int multiplier)
        {
            return new Money(value.Amount * multiplier);
        }

        public static bool operator >(Money left, Money right)
        {
            return left.Amount > right.Amount;
        }

        public static bool operator <(Money left, Money right)
        {
            return left.Amount < right.Amount;
        }

        public static bool operator >=(Money left, Money right)
        {
            return left.Amount >= right.Amount;
        }

        public static bool operator <=(Money left, Money right)
        {
            return left.Amount <= right.Amount;
        }

        public static bool operator ==(Money left, Money right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Money left, Money right)
        {
            return !left.Equals(right);
        }
    }
}
