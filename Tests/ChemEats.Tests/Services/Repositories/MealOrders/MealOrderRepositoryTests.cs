using ChemEats.Tests.TestInfrastructure;
using Domain;
using Domain.Entities;
using Domain.Infrastructure.Identity;
using Domain.Models.Orders;
using Microsoft.EntityFrameworkCore;
using Services.Repositories.MealOrders;

namespace ChemEats.Tests.Services.Repositories.MealOrders;

public sealed class MealOrderRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly MealOrderRepository _repository;

    public MealOrderRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _repository = new MealOrderRepository(_context);
    }

    [Fact]
    public async Task AddAsync_WhenMealOrderIsNull_ShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.AddAsync(null!));
    }

    [Fact]
    public async Task AddAsync_WhenMealDoesNotExist_ShouldThrow()
    {
        MealOrder order = MealOrder.Create("MM", Guid.NewGuid(), DateTime.Today.AddDays(1), 10m);

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => _repository.AddAsync(order));

        Assert.Contains("does not exist", exception.Message);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistOrder_WhenMealExists()
    {
        (ApplicationUser user, Meal meal, Menu menu) = await SeedUserMealMenuAsync();
        MealOrder order = MealOrder.Create(user.Id, meal.Id, menu.Date, 13m);

        await _repository.AddAsync(order);

        MealOrder? persisted = await _context.MealOrders.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == order.Id);
        Assert.NotNull(persisted);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnOrder_WhenExists()
    {
        (ApplicationUser user, Meal meal, Menu menu) = await SeedUserMealMenuAsync();
        MealOrder order = MealOrder.Create(user.Id, meal.Id, menu.Date, 20m);
        await _context.MealOrders.AddAsync(order);
        await _context.SaveChangesAsync();

        MealOrder? result = await _repository.GetByIdAsync(order.Id);

        Assert.NotNull(result);
        Assert.Equal(order.Id, result.Id);
    }

    [Fact]
    public async Task SoftDeleteAsync_ShouldMarkOrderAsDeleted_WhenOrderExists()
    {
        (ApplicationUser user, Meal meal, Menu menu) = await SeedUserMealMenuAsync();
        MealOrder order = MealOrder.Create(user.Id, meal.Id, menu.Date, 20m);
        await _context.MealOrders.AddAsync(order);
        await _context.SaveChangesAsync();

        await _repository.SoftDeleteAsync(order.Id);

        MealOrder? deleted = await _context.MealOrders.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == order.Id);
        Assert.NotNull(deleted);
        Assert.True(deleted.IsDeleted);
    }

    [Fact]
    public async Task GetUserOrdersAsync_ShouldReturnGroupedSummary()
    {
        (ApplicationUser user, Meal meal, Menu menu) = await SeedUserMealMenuAsync();
        MealOrder first = MealOrder.Create(user.Id, meal.Id, menu.Date, 12m);
        MealOrder second = MealOrder.Create(user.Id, meal.Id, menu.Date, 12m);

        await _context.MealOrders.AddRangeAsync(first, second);
        await _context.SaveChangesAsync();

        IReadOnlyList<UserOrderSummary> result = await _repository.GetUserOrdersAsync(user.Id);

        Assert.Equal(2, result.Count);
        Assert.Equal(2, result.Sum(x => x.Quantity));
    }

    [Fact]
    public async Task GetUserOrderItemsAsync_ShouldExcludeDeleted_WhenIncludeDeletedIsFalse()
    {
        (ApplicationUser user, Meal meal, Menu menu) = await SeedUserMealMenuAsync();
        MealOrder active = MealOrder.Create(user.Id, meal.Id, menu.Date, 10m);
        MealOrder deleted = MealOrder.Create(user.Id, meal.Id, menu.Date, 10m);
        deleted.SoftDelete();

        await _context.MealOrders.AddRangeAsync(active, deleted);
        await _context.SaveChangesAsync();

        IReadOnlyList<UserOrderItem> result = await _repository.GetUserOrderItemsAsync(user.Id);

        Assert.Single(result);
        Assert.Equal(active.Id, result[0].OrderId);
    }

    [Fact]
    public async Task GetUserOutstandingSummaryAsync_ShouldReturnOnlyUnpaidCompletedOrders()
    {
        (ApplicationUser user, Meal meal, Menu menu) = await SeedUserMealMenuAsync();

        MealOrder completedUnpaid = MealOrder.Create(user.Id, meal.Id, menu.Date, 20m);
        completedUnpaid.ApplyPortion(5m);
        completedUnpaid.MarkAsCompleted();

        MealOrder completedPaid = MealOrder.Create(user.Id, meal.Id, menu.Date, 10m);
        completedPaid.MarkAsCompleted();
        completedPaid.MarkAsPaid(DateTime.UtcNow);

        await _context.MealOrders.AddRangeAsync(completedUnpaid, completedPaid);
        await _context.SaveChangesAsync();

        UserOutstandingSummary summary = await _repository.GetUserOutstandingSummaryAsync(user.Id);

        Assert.Equal(15m, summary.TotalOutstanding);
        Assert.Equal(1, summary.UnpaidCount);
    }

    [Fact]
    public async Task GetUnpaidOrdersAsync_ShouldFilterBySupplier_WhenSupplierIdIsProvided()
    {
        (ApplicationUser user, Meal meal, Menu menu) = await SeedUserMealMenuAsync();
        Supplier secondSupplier = TestDataFactory.CreateSupplier();
        Menu secondMenu = TestDataFactory.CreateMenu(secondSupplier.Id);
        Meal secondMeal = secondMenu.Meals.Single();

        await _context.Suppliers.AddAsync(secondSupplier);
        await _context.Menus.AddAsync(secondMenu);
        await _context.SaveChangesAsync();

        MealOrder firstOrder = MealOrder.Create(user.Id, meal.Id, menu.Date, 10m);
        MealOrder secondOrder = MealOrder.Create(user.Id, secondMeal.Id, secondMenu.Date, 11m);

        await _context.MealOrders.AddRangeAsync(firstOrder, secondOrder);
        await _context.SaveChangesAsync();

        IReadOnlyList<UserOrderPaymentItem> result = await _repository.GetUnpaidOrdersAsync(user.Id, menu.SupplierId);

        Assert.Single(result);
        Assert.Equal(firstOrder.Id, result[0].OrderId);
    }

    [Fact]
    public async Task MarkAsPaidAsync_WhenOrderDoesNotExist_ShouldThrow()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _repository.MarkAsPaidAsync(Guid.NewGuid(), "missing", DateTime.UtcNow));
    }

    [Fact]
    public async Task MarkAsPaidAsync_ShouldMarkOrderAsPaid()
    {
        (ApplicationUser user, Meal meal, Menu menu) = await SeedUserMealMenuAsync();
        MealOrder order = MealOrder.Create(user.Id, meal.Id, menu.Date, 14m);
        await _context.MealOrders.AddAsync(order);
        await _context.SaveChangesAsync();

        DateTime paidOn = DateTime.UtcNow;
        await _repository.MarkAsPaidAsync(order.Id, user.Id, paidOn);

        MealOrder? paid = await _context.MealOrders.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == order.Id);
        Assert.NotNull(paid);
        Assert.Equal(PaymentStatus.Paid, paid.PaymentStatus);
        Assert.Equal(paidOn, paid.PaidOn);
    }

    [Fact]
    public async Task MarkOrderAsPaidAsync_ShouldApplyPortionOncePerDate_AndReturnTotals()
    {
        (ApplicationUser user, Meal meal, Menu menu) = await SeedUserMealMenuAsync();

        MealOrder first = MealOrder.Create(user.Id, meal.Id, menu.Date, 10m);
        MealOrder second = MealOrder.Create(user.Id, meal.Id, menu.Date, 8m);

        await _context.MealOrders.AddRangeAsync(first, second);
        await _context.SaveChangesAsync();

        (int paidCount, decimal totalPaid) result = await _repository.MarkOrderAsPaidAsync(
            user.Id,
            [first.Id, second.Id],
            DateTime.UtcNow,
            3m);

        Assert.Equal(2, result.paidCount);
        Assert.Equal(15m, result.totalPaid);

        MealOrder persistedFirst = await _context.MealOrders.IgnoreQueryFilters().FirstAsync(x => x.Id == first.Id);
        MealOrder persistedSecond = await _context.MealOrders.IgnoreQueryFilters().FirstAsync(x => x.Id == second.Id);
        Assert.True(persistedFirst.PortionApplied || persistedSecond.PortionApplied);
    }

    [Fact]
    public async Task MarkPendingOrdersAsCompletedForMenuAsync_ShouldCompleteOnlyPendingOrders()
    {
        (ApplicationUser user, Meal meal, Menu menu) = await SeedUserMealMenuAsync();

        MealOrder pending = MealOrder.Create(user.Id, meal.Id, menu.Date, 10m);
        MealOrder cancelled = MealOrder.Create(user.Id, meal.Id, menu.Date, 9m);
        cancelled.Cancel();

        await _context.MealOrders.AddRangeAsync(pending, cancelled);
        await _context.SaveChangesAsync();

        (int completedCount, decimal totalAmount) result = await _repository.MarkPendingOrdersAsCompletedForMenuAsync(menu.Id);

        Assert.Equal(1, result.completedCount);
        Assert.Equal(10m, result.totalAmount);
    }

    [Fact]
    public async Task GetPendingOrdersCountByMenuIdsAsync_ShouldReturnCountPerMenu()
    {
        (ApplicationUser user, Meal meal, Menu menu) = await SeedUserMealMenuAsync();

        MealOrder first = MealOrder.Create(user.Id, meal.Id, menu.Date, 10m);
        MealOrder second = MealOrder.Create(user.Id, meal.Id, menu.Date, 11m);
        MealOrder cancelled = MealOrder.Create(user.Id, meal.Id, menu.Date, 12m);
        cancelled.Cancel();

        await _context.MealOrders.AddRangeAsync(first, second, cancelled);
        await _context.SaveChangesAsync();

        Dictionary<Guid, int> result = await _repository.GetPendingOrdersCountByMenuIdsAsync([menu.Id]);

        Assert.True(result.ContainsKey(menu.Id));
        Assert.Equal(2, result[menu.Id]);
    }

    [Fact]
    public async Task HasPortionAppliedOnDateAsync_ShouldReturnTrue_WhenAnyOrderForUserHasPortionApplied()
    {
        (ApplicationUser user, Meal meal, Menu menu) = await SeedUserMealMenuAsync();
        MealOrder order = MealOrder.Create(user.Id, meal.Id, menu.Date, 10m);
        order.ApplyPortion(2m);

        await _context.MealOrders.AddAsync(order);
        await _context.SaveChangesAsync();

        bool result = await _repository.HasPortionAppliedOnDateAsync(user.Id, DateOnly.FromDateTime(menu.Date));

        Assert.True(result);
    }

    private async Task<(ApplicationUser User, Meal Meal, Menu Menu)> SeedUserMealMenuAsync()
    {
        Supplier supplier = TestDataFactory.CreateSupplier();
        Menu menu = TestDataFactory.CreateMenu(supplier.Id);
        Meal meal = menu.Meals.Single();
        ApplicationUser user = TestDataFactory.CreateUser(Guid.NewGuid().ToString(), "USR", "Repository User");

        await _context.Suppliers.AddAsync(supplier);
        await _context.Users.AddAsync(user);
        await _context.Menus.AddAsync(menu);
        await _context.SaveChangesAsync();

        return (user, meal, menu);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
