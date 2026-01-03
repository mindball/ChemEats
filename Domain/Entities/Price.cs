using System.Globalization;

namespace Domain.Entities;

public sealed class Price
{
    public decimal Amount { get; }

    public Price(decimal amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Amount = amount;
    }

    public IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
    }

    // Use current culture currency formatting (will show € because Program.cs sets it)
    public override string ToString() => Amount.ToString("C", CultureInfo.CurrentCulture);

    // Implicit conversion from decimal to Price
    public static implicit operator Price(decimal amount) => new(amount);

    // Explicit conversion from Price to decimal
    public static explicit operator decimal(Price price) => price.Amount;

    public override bool Equals(object? obj) =>
        obj is Price other && Amount == other.Amount;

    public override int GetHashCode() => Amount.GetHashCode();
}