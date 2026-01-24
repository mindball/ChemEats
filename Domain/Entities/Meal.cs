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

    public string Name { get; private set; }
    public Price Price { get; private set; }

    public static Meal Create(Guid menuId, string name, Price price)
    {

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Meal name is required");

        return price is null ? throw new ArgumentNullException(nameof(price)) : new Meal(Guid.NewGuid(), menuId, name.Trim(), price);
    }

    public void ChangePrice(Price newPrice)
    {
        Price = newPrice;
    }
}