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
            throw new DomainException("Supplier is required.");

        if (date.Date <= DateTime.Today)
            throw new DomainException("Menu date must be in the future.");

        List<Meal> mealList = meals.ToList();
        if (mealList.Count == 0)
            throw new DomainException("Menu must contain at least one meal.");

        SupplierId = supplierId;
        Date = date;
        ActiveUntil = activeUntil;
        RegisterDate = DateTime.Now;
        _meals = mealList;
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
        if (IsDeleted)
            throw new DomainException("Cannot update deleted menu.");

        if (!IsActive())
            throw new DomainException("Cannot update date of inactive menu.");

        if (newDate.Date <= DateTime.Today)
            throw new DomainException("Menu date must be in the future.");

        Date = newDate;
    }

    public void UpdateActiveUntil(DateTime newActiveUntil)
    {
        if (IsDeleted)
            throw new DomainException("Cannot update deleted menu.");

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
        return !IsDeleted && DateTime.Now <= ActiveUntil;
    }

    private void ValidateActiveUntil()
    {
        if (ActiveUntil <= DateTime.Now)
            throw new DomainException("ActiveUntil must be in the future.");

        TimeSpan timeOfDay = ActiveUntil.TimeOfDay;
        if (timeOfDay < TimeSpan.FromHours(8) || timeOfDay > TimeSpan.FromHours(16))
            throw new DomainException("ActiveUntil must be between 08:00 and 16:00.");
    }

    public void SoftDeleteWithPendingOrders(IReadOnlyCollection<MealOrder> orders)
    {
        if (IsDeleted)
            throw new DomainException("Menu is already deleted.");

        if (!IsActive())
            throw new DomainException("Cannot delete inactive menu.");

        if (Date < DateTime.Today)
            throw new DomainException("Past menus cannot be deleted.");

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

    public void FinalizeMenu()
    {
        if (IsDeleted)
            throw new DomainException("Cannot finalize deleted menu.");

        if (!IsActive())
            throw new DomainException("Menu is already inactive.");

        if (Date.Date <= DateTime.Today)
            throw new DomainException("Can only finalize future menu.");

        ActiveUntil = DateTime.Now;
    }
}