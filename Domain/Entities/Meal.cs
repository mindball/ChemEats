using StronglyTypedIds;

namespace Domain.Entities;

[StronglyTypedId(Template.Guid)]
public partial struct MealId {}

public class Meal : Entity<MealId>
{
    private Meal() { } // EF

    public Meal(MealId id, string name, Price price)
    {
        Id = id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Price = price ?? throw new ArgumentNullException(nameof(price));
    }

    public static Meal Create(string name, Price price)
    {
        return new Meal(MealId.New(), name, price);
    }

    public string Name { get; private set; }
    public Price Price { get; private set; }

    public void ChangePrice(Price newPrice)
    {
        Price = newPrice;
    }
}