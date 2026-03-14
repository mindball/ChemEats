using Domain.Entities;
using Domain.Infrastructure.Exceptions;

namespace ChemEats.Tests.Domain.Entities;

public class MenuTests
{
    [Fact]
    public void Constructor_WhenSupplierIdIsEmpty_ShouldThrow()
    {
        List<Meal> meals = [Meal.Create(Guid.NewGuid(), "Soup", new Price(7m))];

        DomainException exception = Assert.Throws<DomainException>(() =>
            new Menu(Guid.NewGuid(), Guid.Empty, DateTime.Today.AddDays(2), GetValidActiveUntil(), meals));

        Assert.Equal("Supplier is required.", exception.Message);
    }

    [Fact]
    public void Constructor_WhenDateIsToday_ShouldThrow()
    {
        List<Meal> meals = [Meal.Create(Guid.NewGuid(), "Soup", new Price(7m))];

        DomainException exception = Assert.Throws<DomainException>(() =>
            new Menu(Guid.NewGuid(), Guid.NewGuid(), DateTime.Today, GetValidActiveUntil(), meals));

        Assert.Equal("Menu date must be in the future.", exception.Message);
    }

    [Fact]
    public void Constructor_WhenMealsCollectionIsEmpty_ShouldThrow()
    {
        List<Meal> meals = [];

        DomainException exception = Assert.Throws<DomainException>(() =>
            new Menu(Guid.NewGuid(), Guid.NewGuid(), DateTime.Today.AddDays(2), GetValidActiveUntil(), meals));

        Assert.Equal("Menu must contain at least one meal.", exception.Message);
    }

    [Fact]
    public void Constructor_WhenActiveUntilIsNotInFuture_ShouldThrow()
    {
        List<Meal> meals = [Meal.Create(Guid.NewGuid(), "Soup", new Price(7m))];

        DomainException exception = Assert.Throws<DomainException>(() =>
            new Menu(Guid.NewGuid(), Guid.NewGuid(), DateTime.Today.AddDays(2), DateTime.Today.AddDays(-1).AddHours(12), meals));

        Assert.Equal("ActiveUntil must be in the future.", exception.Message);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(17)]
    public void Constructor_WhenActiveUntilIsOutsideWorkingWindow_ShouldThrow(int hour)
    {
        List<Meal> meals = [Meal.Create(Guid.NewGuid(), "Soup", new Price(7m))];
        DateTime activeUntil = DateTime.Today.AddDays(1).AddHours(hour);

        DomainException exception = Assert.Throws<DomainException>(() =>
            new Menu(Guid.NewGuid(), Guid.NewGuid(), DateTime.Today.AddDays(2), activeUntil, meals));

        Assert.Equal("ActiveUntil must be between 08:00 and 16:00.", exception.Message);
    }

    [Fact]
    public void Create_ShouldCopyMealsAndAssignMenuIdToEveryMeal()
    {
        Meal sourceMeal = Meal.Create(Guid.NewGuid(), "Pasta", new Price(12m));

        Menu menu = Menu.Create(Guid.NewGuid(), DateTime.Today.AddDays(2), GetValidActiveUntil(), [sourceMeal]);

        Assert.Single(menu.Meals);
        Meal menuMeal = menu.Meals.Single();
        Assert.Equal(menu.Id, menuMeal.MenuId);
        Assert.Equal("Pasta", menuMeal.Name);
        Assert.Equal(12m, menuMeal.Price.Amount);
    }

    [Fact]
    public void UpdateDate_WhenDateIsNotFuture_ShouldThrow()
    {
        Menu menu = CreateActiveMenu();

        DomainException exception = Assert.Throws<DomainException>(() => menu.UpdateDate(DateTime.Today));

        Assert.Equal("Menu date must be in the future.", exception.Message);
    }

    [Fact]
    public void UpdateDateWithPendingOrders_ShouldUpdateOnlyPendingAndNotDeletedOrders()
    {
        Menu menu = CreateActiveMenu();
        DateTime newDate = DateTime.Today.AddDays(4);

        MealOrder pendingOrder = MealOrder.Create("MM", Guid.NewGuid(), DateTime.Today.AddDays(2), 10m);
        MealOrder cancelledOrder = MealOrder.Create("DM", Guid.NewGuid(), DateTime.Today.AddDays(2), 10m);
        cancelledOrder.Cancel();

        MealOrder deletedOrder = MealOrder.Create("AB", Guid.NewGuid(), DateTime.Today.AddDays(2), 10m);
        deletedOrder.SoftDelete();

        menu.UpdateDateWithPendingOrders(newDate, [pendingOrder, cancelledOrder, deletedOrder]);

        Assert.Equal(newDate, menu.Date);
        Assert.Equal(newDate, pendingOrder.MenuDate);
        Assert.Equal(DateTime.Today.AddDays(2), cancelledOrder.MenuDate);
        Assert.Equal(DateTime.Today.AddDays(2), deletedOrder.MenuDate);
    }

    [Fact]
    public void UpdateActiveUntil_WhenValidationFails_ShouldRestoreOriginalValue()
    {
        Menu menu = CreateActiveMenu();
        DateTime originalActiveUntil = menu.ActiveUntil;

        DomainException exception = Assert.Throws<DomainException>(() =>
            menu.UpdateActiveUntil(DateTime.Today.AddDays(1).AddHours(18)));

        Assert.Equal("ActiveUntil must be between 08:00 and 16:00.", exception.Message);
        Assert.Equal(originalActiveUntil, menu.ActiveUntil);
    }

    [Fact]
    public void SoftDeleteWithPendingOrders_WhenCompletedOrderExists_ShouldThrow()
    {
        Menu menu = CreateActiveMenu();
        MealOrder completedOrder = MealOrder.Create("MM", Guid.NewGuid(), DateTime.Today.AddDays(2), 10m);
        completedOrder.MarkAsCompleted();

        DomainException exception = Assert.Throws<DomainException>(() =>
            menu.SoftDeleteWithPendingOrders([completedOrder]));

        Assert.Equal("Menu cannot be deleted while there are completed orders.", exception.Message);
        Assert.False(menu.IsDeleted);
    }

    [Fact]
    public void SoftDeleteWithPendingOrders_WhenNoCompletedOrders_ShouldDeleteMenuAndPendingOrders()
    {
        Menu menu = CreateActiveMenu();

        MealOrder pendingOrder = MealOrder.Create("MM", Guid.NewGuid(), DateTime.Today.AddDays(2), 10m);
        MealOrder cancelledOrder = MealOrder.Create("DM", Guid.NewGuid(), DateTime.Today.AddDays(2), 10m);
        cancelledOrder.Cancel();

        menu.SoftDeleteWithPendingOrders([pendingOrder, cancelledOrder]);

        Assert.True(menu.IsDeleted);
        Assert.True(pendingOrder.IsDeleted);
        Assert.False(cancelledOrder.IsDeleted);
    }

    [Fact]
    public void FinalizeMenu_WhenMenuIsDeleted_ShouldThrow()
    {
        Menu menu = CreateActiveMenu();
        menu.SoftDeleteWithPendingOrders([]);

        DomainException exception = Assert.Throws<DomainException>(() => menu.FinalizeMenu());

        Assert.Equal("Cannot finalize deleted menu.", exception.Message);
    }

    [Fact]
    public void FinalizeMenu_WhenMenuIsActive_ShouldSetActiveUntilToNowOrEarlier()
    {
        Menu menu = CreateActiveMenu();

        menu.FinalizeMenu();

        Assert.True(menu.ActiveUntil <= DateTime.Now);
    }

    private static Menu CreateActiveMenu()
    {
        Guid supplierId = Guid.NewGuid();
        Guid menuId = Guid.NewGuid();
        DateTime date = DateTime.Today.AddDays(2);
        DateTime activeUntil = GetValidActiveUntil();
        List<Meal> meals = [Meal.Create(Guid.NewGuid(), "Soup", new Price(7m))];

        return new Menu(menuId, supplierId, date, activeUntil, meals);
    }

    private static DateTime GetValidActiveUntil()
    {
        return DateTime.Today.AddDays(1).AddHours(12);
    }
}
