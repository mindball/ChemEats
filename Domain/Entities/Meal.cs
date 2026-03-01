using Domain.Infrastructure.Exceptions;

namespace Domain.Entities;

public class Meal 
{
    private Meal() { }

    internal Meal(Guid id, Guid menuId, string name, Price price)
    {
        Id = id;
        MenuId = menuId;
        Name = name;
        Price = price;
    }

    public Guid Id { get; private set; }

    public Guid MenuId { get; private set; }
    public Menu Menu { get; private set; } = null!;

    public string Name { get; private set; } = null!;
    public Price Price { get; private set; } = null!;

    public static Meal Create(Guid menuId, string name, Price price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Meal name is required.");

        ArgumentNullException.ThrowIfNull(price);

        return new Meal(Guid.NewGuid(), menuId, name.Trim(), price);
    }

    public void ChangePrice(Price newPrice)
    {
        ArgumentNullException.ThrowIfNull(newPrice);
        Price = newPrice;
    }
}