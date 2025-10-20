using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class Menu
{
    private readonly List<Meal> _meals = [];

    private Menu()
    {
    }

    public Menu(Guid id, Guid supplierId, DateTime date, IEnumerable<Meal> meals)
    {
        Id = id;
        SupplierId = supplierId;
        Date = date;
        _meals = meals.ToList();
    }

    public Guid Id { get; private set; }

    [Required] public Guid SupplierId { get; private set; }

    public Supplier? Supplier { get; private set; }

    [Required] public DateTime Date { get; private set; }

    public IReadOnlyCollection<Meal> Meals => _meals.AsReadOnly();

    public static Menu Create(Guid supplierId, DateTime date, IEnumerable<Meal> meals)
    {
        return new Menu(Guid.NewGuid(), supplierId, date, meals);
    }
}