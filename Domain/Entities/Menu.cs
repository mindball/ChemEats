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
        RegisterDate = DateTime.Now;
        _meals = meals.ToList();
        IsDeleted = false;
    }

    public Guid Id { get; private set; }

    [Required] public Guid SupplierId { get; private set; }

    public Supplier? Supplier { get; private set; }

    [Required] public DateTime Date { get; private set; }

    [Required] public DateTime RegisterDate { get; private set; }


    public bool IsDeleted { get; private set; }

    public IReadOnlyCollection<Meal> Meals => _meals.AsReadOnly();

    public static Menu Create(Guid supplierId, DateTime date, IEnumerable<Meal> meals)
    {
        Guid menuId = Guid.NewGuid();

        IEnumerable<Meal> fixedMeals = meals.Select(m =>
            Meal.Create(menuId, m.Name, m.Price)
        );

        return new Menu(menuId, supplierId, date, fixedMeals);
    }

    public void UpdateDate(DateTime newDate)
    {
        Date = newDate;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
    }
}