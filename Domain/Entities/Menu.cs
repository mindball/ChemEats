using Domain.Infrastructure.Exceptions;

namespace Domain.Entities;

public class Menu
{
    private readonly List<Meal> _meals = [];

    private Menu()
    {
    }

    public Menu(Guid id, Guid supplierId, DateTime date, DateTime activeUntil, IEnumerable<Meal> meals)
    {
        Id = id;

        if (supplierId == Guid.Empty)
            throw new DomainException("Supplier is required");


        SupplierId = supplierId;
        Date = date;
        ActiveUntil = activeUntil;
        RegisterDate = DateTime.Now;
        _meals = meals.ToList();
        IsDeleted = false;

        ValidateActiveUntil();
    }

    public Guid Id { get; private set; }

    public Guid SupplierId { get; private set; }

    public Supplier? Supplier { get; private set; }

    public DateTime Date { get; private set; }

    public DateTime ActiveUntil { get; private set; }

    public DateTime RegisterDate { get; private set; }


    public bool IsDeleted { get; private set; }

    public IReadOnlyCollection<Meal> Meals => _meals.AsReadOnly();

    public static Menu Create(Guid supplierId, DateTime date, DateTime activeUntil, IEnumerable<Meal> meals)
    {
        Guid menuId = Guid.NewGuid();

        IEnumerable<Meal> fixedMeals = meals.Select(m =>
            Meal.Create(menuId, m.Name, m.Price)
        );

        return new Menu(menuId, supplierId, date, activeUntil, fixedMeals);
    }

    public void UpdateDate(DateTime newDate)
    {
        if (newDate < DateTime.UtcNow.Date)
            throw new DomainException("Menu date cannot be in the past.");
        
        Date = newDate;
    }

    public void UpdateActiveUntil(DateTime newActiveUntil)
    {
        DateTime oldActiveUntil = ActiveUntil;
        ActiveUntil = newActiveUntil;

        try
        {
            ValidateActiveUntil();
        }
        catch
        {
            ActiveUntil = oldActiveUntil;
            throw;
        }
    }

    public bool IsActive()
    {
        DateTime now = DateTime.Now;
        return !IsDeleted && 
               Date.Date == now.Date && 
               now <= ActiveUntil;
    }

    private void ValidateActiveUntil()
    {
        if (ActiveUntil.Date != RegisterDate.Date)
            throw new DomainException("ActiveUntil must be on the same day as the register menu date.");

        if (ActiveUntil <= DateTime.Now && Date.Date == DateTime.Today)
            throw new DomainException("ActiveUntil must be in the future for today's menu.");

        TimeSpan timeOfDay = ActiveUntil.TimeOfDay;
        if (timeOfDay < TimeSpan.FromHours(8) || timeOfDay > TimeSpan.FromHours(16))
            throw new DomainException("ActiveUntil must be between 08:00 and 16:00.");
    }

    public void SoftDeleteWithPendingOrders(IReadOnlyCollection<MealOrder> orders)
    {
        if (IsDeleted) return;

        if (Date < DateTime.Today)
            throw new DomainException("Past menus cannot be deleted");

        if (orders.Any(o =>
                o is { IsDeleted: false, Status: MealOrderStatus.Completed }))
        {
            throw new DomainException(
                "Menu cannot be deleted while there are completed orders.");
        }

        IsDeleted = true;

        foreach (MealOrder order in orders.Where(o =>
                     o is { IsDeleted: false, Status: MealOrderStatus.Pending }))
        {
            order.SoftDelete();
        }
    }
}