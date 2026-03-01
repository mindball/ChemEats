using System.Globalization;

namespace Domain.Entities;

public sealed class Price : IEquatable<Price>, IComparable<Price>
{
    public decimal Amount { get; }

    public Price(decimal amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Price cannot be negative.");
        Amount = amount;
    }

    public static Price Zero => new(0m);

    public Price Add(Price other) => new(Amount + other.Amount);

    public Price Subtract(Price other)
    {
        decimal result = Amount - other.Amount;
        return new(Math.Max(0m, result));
    }

    public override string ToString() => Amount.ToString("C", CultureInfo.CurrentCulture);

    public static implicit operator Price(decimal amount) => new(amount);

    public static explicit operator decimal(Price price) => price.Amount;

    public bool Equals(Price? other) => other is not null && Amount == other.Amount;

    public override bool Equals(object? obj) => Equals(obj as Price);

    public override int GetHashCode() => Amount.GetHashCode();

    public int CompareTo(Price? other) => other is null ? 1 : Amount.CompareTo(other.Amount);

    public static bool operator ==(Price? left, Price? right) => Equals(left, right);

    public static bool operator !=(Price? left, Price? right) => !Equals(left, right);

    public static bool operator >(Price left, Price right) => left.Amount > right.Amount;

    public static bool operator <(Price left, Price right) => left.Amount < right.Amount;

    public static bool operator >=(Price left, Price right) => left.Amount >= right.Amount;

    public static bool operator <=(Price left, Price right) => left.Amount <= right.Amount;
}
