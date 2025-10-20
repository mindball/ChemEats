using StronglyTypedIds;

namespace Domain.Entities;

public class Meal 
{
    private Meal() { } // EF

    public Meal(Guid id, string name, Price price)
    {
        Id = id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Price = price ?? throw new ArgumentNullException(nameof(price));
    }

    public static Meal Create(string name, Price price)
    {
        return new Meal(Guid.NewGuid(), name, price);
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public Price Price { get; private set; }

    public void ChangePrice(Price newPrice)
    {
        Price = newPrice;
    }
}