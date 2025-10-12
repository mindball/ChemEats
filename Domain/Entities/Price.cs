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

    public override string ToString() => $"{Amount:F2} лв.";

    // Implicit conversion from decimal to Price
    public static implicit operator Price(decimal amount) => new(amount);

    // Explicit conversion from Price to decimal
    public static explicit operator decimal(Price price) => price.Amount;
}