using System.ComponentModel.DataAnnotations;
using Domain.Infrastructure.Exceptions;

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

        if (supplierId == Guid.Empty)
            throw new DomainException("Supplier is required");


        SupplierId = supplierId;
        Date = date;
        RegisterDate = DateTime.Now;
        _meals = meals.ToList();
        IsDeleted = false;
    }

    public Guid Id { get; private set; }

    public Guid SupplierId { get; private set; }

    public Supplier? Supplier { get; private set; }

    public DateTime Date { get; private set; }

    public DateTime RegisterDate { get; private set; }


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
        if (newDate < DateTime.UtcNow.Date)
            throw new DomainException("Menu date cannot be in the past.");
    }

    private void SoftDeleteInternal()
    {
        if (Date < DateTime.Today)
            throw new DomainException("Past menus cannot be deleted");
        IsDeleted = true;
    }

    public void SoftDelete()
    {
        if (IsDeleted) return;
        SoftDeleteInternal();
    }

    public void SoftDeleteAndCancelOrders(IEnumerable<MealOrder> orders)
    {
        if (IsDeleted) return;

        SoftDeleteInternal();

        foreach (MealOrder order in orders)
            order.Cancel();
    }

    public void EnsureNoPendingNonDeletedOrders(IEnumerable<MealOrder> orders)
    {
        bool hasBlockingOrders = orders.Any(o => o is { IsDeleted: false, Status: MealOrderStatus.Pending } && o.Meal.MenuId == Id);
        if (hasBlockingOrders)
            throw new DomainException("Menu cannot be deleted while there are pending orders.");
    }
    
    public void SoftDeleteIfNoActiveOrders(IEnumerable<MealOrder> orders)
    {
        if (IsDeleted) return;
        EnsureNoPendingNonDeletedOrders(orders);
        SoftDeleteInternal();
    }
}