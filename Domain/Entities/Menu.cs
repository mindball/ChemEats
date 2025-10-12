using System.ComponentModel.DataAnnotations;
using StronglyTypedIds;

namespace Domain.Entities;

[StronglyTypedId(Template.Guid)]
public partial struct MenuId
{
}

public class Menu : Entity<MenuId>
{
    private readonly List<Meal> _meals = [];

    private Menu()
    {
    }

    public Menu(MenuId id, SupplierId supplierId, DateTime date, IEnumerable<Meal> meals)
    {
        Id = id;
        SupplierId = supplierId;
        Date = date;
        _meals = meals.ToList();
    }

    [Required] public SupplierId SupplierId { get; private set; }

    public Supplier? Supplier { get; private set; }

    [Required] public DateTime Date { get; private set; }

    public IReadOnlyCollection<Meal> Meals => _meals.AsReadOnly();

    public static Menu Create(SupplierId supplierId, DateTime date, IEnumerable<Meal> meals)
    {
        return new Menu(MenuId.New(), supplierId, date, meals);
    }
}