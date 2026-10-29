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
        completedPaid.MarkAsPaid(DateTime.UtcNow, "ADMIN");

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
            _repository.MarkAsPaidAsync(Guid.NewGuid(), "missing", DateTime.UtcNow, "ADMIN"));
    }

    [Fact]
    public async Task MarkAsPaidAsync_ShouldMarkOrderAsPaid()
    {
        (ApplicationUser user, Meal meal, Menu menu) = await SeedUserMealMenuAsync();
        MealOrder order = MealOrder.Create(user.Id, meal.Id, menu.Date, 14m);
        order.MarkAsCompleted();
        await _context.MealOrders.AddAsync(order);
        await _context.SaveChangesAsync();

        DateTime paidOn = DateTime.UtcNow;
        await _repository.MarkAsPaidAsync(order.Id, user.Id, paidOn, "ADMIN");

        MealOrder? paid = await _context.MealOrders.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == order.Id);
        Assert.NotNull(paid);
        Assert.Equal(PaymentStatus.Paid, paid.PaymentStatus);
        Assert.Equal(paidOn, paid.PaidOn);
        Assert.Equal("ADMIN", paid.PaidByUserId);
    }

    [Fact]
    public async Task MarkOrderAsPaidAsync_ShouldApplyPortionOncePerDateToMostExpensiveOrder_AndReturnTotals()
    {
        (ApplicationUser user, Meal meal, Menu menu) = await SeedUserMealMenuAsync();

        MealOrder first = TestDataFactory.CreateCompletedOrder(user.Id, meal.Id, menu.Date, 10m);
        MealOrder second = TestDataFactory.CreateCompletedOrder(user.Id, meal.Id, menu.Date, 8m);

        await _context.MealOrders.AddRangeAsync(first, second);
        await _context.SaveChangesAsync();

        (int paidCount, decimal totalPaid) result = await _repository.MarkOrderAsPaidAsync(
            user.Id,
            [first.Id, second.Id],
            DateTime.UtcNow,
            3m,
            "ADMIN");

        Assert.Equal(2, result.paidCount);
        Assert.Equal(15m, result.totalPaid);

        MealOrder persistedFirst = await _context.MealOrders.IgnoreQueryFilters().FirstAsync(x => x.Id == first.Id);
        MealOrder persistedSecond = await _context.MealOrders.IgnoreQueryFilters().FirstAsync(x => x.Id == second.Id);
        Assert.True(persistedFirst.PortionApplied);
        Assert.Equal(3m, persistedFirst.PortionAmount);
        Assert.False(persistedSecond.PortionApplied);
        Assert.Equal("ADMIN", persistedFirst.PaidByUserId);
        Assert.Equal("ADMIN", persistedSecond.PaidByUserId);
    }

    [Fact]
    public async Task MarkOrderAsPaidAsync_WhenListContainsPendingOrCancelledOrders_ShouldPayOnlyCompletedWithoutThrowing()
    {
        (ApplicationUser user, Meal meal, Menu menu) = await SeedUserMealMenuAsync();

        MealOrder unpaid = TestDataFactory.CreateCompletedOrder(user.Id, meal.Id, menu.Date, 10m);
        MealOrder pending = MealOrder.Create(user.Id, meal.Id, menu.Date, 9m);
        MealOrder cancelled = MealOrder.Create(user.Id, meal.Id, menu.Date, 8m);
        cancelled.Cancel();

        await _context.MealOrders.AddRangeAsync(unpaid, pending, cancelled);
        await _context.SaveChangesAsync();

        (int paidCount, decimal totalPaid) result = await _repository.MarkOrderAsPaidAsync(
            user.Id,
            [unpaid.Id, pending.Id, cancelled.Id],
            DateTime.UtcNow,
            0m,
            "ADMIN");

        Assert.Equal(1, result.paidCount);
        Assert.Equal(10m, result.totalPaid);

        MealOrder persistedCancelled = await _context.MealOrders.IgnoreQueryFilters().FirstAsync(x => x.Id == cancelled.Id);
        MealOrder persistedPending = await _context.MealOrders.IgnoreQueryFilters().FirstAsync(x => x.Id == pending.Id);
        Assert.Equal(PaymentStatus.Unpaid, persistedCancelled.PaymentStatus);
        Assert.Equal(PaymentStatus.Unpaid, persistedPending.PaymentStatus);
    }

    [Fact]
    public async Task MarkAllUnpaidAsPaidAsync_WhenUnpaidOrdersExistUpToDate_ShouldPayThemIncludingEarlierOnes()
    {
        (ApplicationUser user, Meal meal, Menu _) = await SeedUserMealMenuAsync();
        ApplicationUser otherUser = await SeedUserAsync("OTH", "Other User");
        DateTime upToDate = new(2026, 10, 9);

        MealOrder earlier = TestDataFactory.CreateCompletedOrder(user.Id, meal.Id, new DateTime(2026, 9, 15), 7m);
        MealOrder inRange = TestDataFactory.CreateCompletedOrder(user.Id, meal.Id, upToDate, 10m);
        MealOrder afterDate = TestDataFactory.CreateCompletedOrder(user.Id, meal.Id, upToDate.AddDays(1), 11m);

        MealOrder cancelled = TestDataFactory.CreateOrder(user.Id, meal.Id, upToDate, 12m);
        cancelled.Cancel();

        MealOrder deleted = TestDataFactory.CreateOrder(user.Id, meal.Id, upToDate, 13m);
        deleted.SoftDelete();

        MealOrder alreadyPaid = TestDataFactory.CreateCompletedOrder(user.Id, meal.Id, new DateTime(2026, 9, 1), 14m);
        alreadyPaid.MarkAsPaid(new DateTime(2026, 9, 2), "EARLIER-ADMIN");

        MealOrder otherUsersOrder = TestDataFactory.CreateCompletedOrder(otherUser.Id, meal.Id, upToDate, 15m);
        MealOrder pendingInRange = TestDataFactory.CreateOrder(user.Id, meal.Id, upToDate, 16m);

        await _context.MealOrders.AddRangeAsync(earlier, inRange, afterDate, cancelled, deleted, alreadyPaid, otherUsersOrder, pendingInRange);
        await _context.SaveChangesAsync();

        (int paidCount, decimal totalPaid) result = await _repository.MarkAllUnpaidAsPaidAsync(
            user.Id,
            upToDate,
            new DateTime(2026, 10, 10, 8, 0, 0),
            0m,
            "ADMIN");

        Assert.Equal(2, result.paidCount);
        Assert.Equal(17m, result.totalPaid);

        List<MealOrder> persisted = await _context.MealOrders.IgnoreQueryFilters().ToListAsync();
        Assert.Equal(PaymentStatus.Paid, persisted.Single(x => x.Id == earlier.Id).PaymentStatus);
        Assert.Equal(PaymentStatus.Paid, persisted.Single(x => x.Id == inRange.Id).PaymentStatus);
        Assert.Equal("ADMIN", persisted.Single(x => x.Id == inRange.Id).PaidByUserId);
        Assert.Equal(PaymentStatus.Unpaid, persisted.Single(x => x.Id == afterDate.Id).PaymentStatus);
        Assert.Equal(PaymentStatus.Unpaid, persisted.Single(x => x.Id == cancelled.Id).PaymentStatus);
        Assert.Equal(PaymentStatus.Unpaid, persisted.Single(x => x.Id == deleted.Id).PaymentStatus);
        Assert.Equal("EARLIER-ADMIN", persisted.Single(x => x.Id == alreadyPaid.Id).PaidByUserId);
        Assert.Equal(PaymentStatus.Unpaid, persisted.Single(x => x.Id == otherUsersOrder.Id).PaymentStatus);
        Assert.Equal(PaymentStatus.Unpaid, persisted.Single(x => x.Id == pendingInRange.Id).PaymentStatus);
    }

    [Fact]
    public async Task MarkAllUnpaidAsPaidAsync_WhenPortionIsSet_ShouldApplyItToMostExpensiveOrderOfEachDay()
    {
        (ApplicationUser user, Meal meal, Menu _) = await SeedUserMealMenuAsync();
        DateTime day = new(2026, 10, 6);

        MealOrder cheap = TestDataFactory.CreateCompletedOrder(user.Id, meal.Id, day, 4m);
        MealOrder expensive = TestDataFactory.CreateCompletedOrder(user.Id, meal.Id, day, 9m);

        await _context.MealOrders.AddRangeAsync(cheap, expensive);
        await _context.SaveChangesAsync();

        (int paidCount, decimal totalPaid) result = await _repository.MarkAllUnpaidAsPaidAsync(
            user.Id,
            day,
            DateTime.Now,
            5m,
            "ADMIN");

        Assert.Equal(2, result.paidCount);
        Assert.Equal(8m, result.totalPaid);

        MealOrder persistedExpensive = await _context.MealOrders.FirstAsync(x => x.Id == expensive.Id);
        MealOrder persistedCheap = await _context.MealOrders.FirstAsync(x => x.Id == cheap.Id);
        Assert.Equal(5m, persistedExpensive.PortionAmount);
        Assert.False(persistedCheap.PortionApplied);
    }

    [Fact]
    public async Task MarkAllUnpaidAsPaidAsync_WhenNothingIsUnpaid_ShouldReturnZero()
    {
        (ApplicationUser user, Meal _, Menu _) = await SeedUserMealMenuAsync();

        (int paidCount, decimal totalPaid) result = await _repository.MarkAllUnpaidAsPaidAsync(
            user.Id,
            new DateTime(2026, 10, 9),
            DateTime.Now,
            3m,
            "ADMIN");

        Assert.Equal(0, result.paidCount);
        Assert.Equal(0m, result.totalPaid);
    }

    [Fact]
    public async Task GetAllOrdersForPeriodAsync_ShouldFilterByMenuDateAndFlagCancelledAndPaidBy()
    {
        (ApplicationUser user, Meal meal, Menu _) = await SeedUserMealMenuAsync();
        ApplicationUser admin = await SeedUserAsync("ADM", "Admin Person");
        DateTime startDate = new(2026, 10, 5);
        DateTime endDate = new(2026, 10, 9);

        // OrderedAt is "now", outside the period; only MenuDate decides.
        MealOrder paid = TestDataFactory.CreateCompletedOrder(user.Id, meal.Id, startDate, 10m);
        paid.MarkAsPaid(DateTime.Now, admin.Id);

        MealOrder cancelled = TestDataFactory.CreateOrder(user.Id, meal.Id, endDate, 8m);
        cancelled.Cancel();

        MealOrder outside = TestDataFactory.CreateCompletedOrder(user.Id, meal.Id, endDate.AddDays(1), 9m);

        await _context.MealOrders.AddRangeAsync(paid, cancelled, outside);
        await _context.SaveChangesAsync();

        IReadOnlyList<UserOrderPaymentItem> result =
            await _repository.GetAllOrdersForPeriodAsync(user.Id, startDate, endDate);

        Assert.Equal(2, result.Count);

        UserOrderPaymentItem paidItem = result.Single(x => x.OrderId == paid.Id);
        Assert.Equal("Admin Person", paidItem.PaidByName);
        Assert.False(paidItem.IsCancelled);
        Assert.True(paidItem.IsCompleted);

        UserOrderPaymentItem cancelledItem = result.Single(x => x.OrderId == cancelled.Id);
        Assert.True(cancelledItem.IsCancelled);
        Assert.False(cancelledItem.IsCompleted);
        Assert.Null(cancelledItem.PaidByName);
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

    [Fact]
    public async Task GetOutstandingSummariesAsync_WhenUsersHaveUnpaidOrdersInPeriod_ShouldGroupPerEmployeeSortedByName()
    {
        (ApplicationUser repositoryUser, Meal meal, Menu _) = await SeedUserMealMenuAsync();
        ApplicationUser alpha = await SeedUserAsync("ALP", "Alpha User");
        DateTime firstDay = new(2026, 10, 5);
        DateTime secondDay = new(2026, 10, 6);

        MealOrder alphaFirstDayA = TestDataFactory.CreateCompletedOrder(alpha.Id, meal.Id, firstDay, 10m);
        MealOrder alphaFirstDayB = TestDataFactory.CreateCompletedOrder(alpha.Id, meal.Id, firstDay, 8m);
        MealOrder alphaSecondDay = TestDataFactory.CreateCompletedOrder(alpha.Id, meal.Id, secondDay, 12m);
        MealOrder repositoryUserFirstDay = TestDataFactory.CreateCompletedOrder(repositoryUser.Id, meal.Id, firstDay, 9m);

        await _context.MealOrders.AddRangeAsync(alphaFirstDayA, alphaFirstDayB, alphaSecondDay, repositoryUserFirstDay);
        await _context.SaveChangesAsync();

        IReadOnlyList<EmployeeOutstandingSummary> result =
            await _repository.GetOutstandingSummariesAsync(firstDay, secondDay, 3m);

        Assert.Equal(2, result.Count);

        EmployeeOutstandingSummary alphaSummary = result[0];
        Assert.Equal(alpha.Id, alphaSummary.UserId);
        Assert.Equal("Alpha User", alphaSummary.EmployeeName);
        Assert.Equal(3, alphaSummary.UnpaidCount);
        Assert.Equal(30m, alphaSummary.GrossAmount);
        Assert.Equal(6m, alphaSummary.PortionAmount);
        Assert.Equal(24m, alphaSummary.NetAmount);

        EmployeeOutstandingSummary repositoryUserSummary = result[1];
        Assert.Equal(repositoryUser.Id, repositoryUserSummary.UserId);
        Assert.Equal(1, repositoryUserSummary.UnpaidCount);
        Assert.Equal(9m, repositoryUserSummary.GrossAmount);
        Assert.Equal(3m, repositoryUserSummary.PortionAmount);
        Assert.Equal(6m, repositoryUserSummary.NetAmount);
    }

    [Fact]
    public async Task GetOutstandingSummariesAsync_WhenOrdersArePaidPendingDeletedCancelledOrAfterPeriod_ShouldExcludeThem_AndReportEarlierSeparately()
    {
        (ApplicationUser user, Meal meal, Menu _) = await SeedUserMealMenuAsync();
        DateTime startDate = new(2026, 10, 5);
        DateTime endDate = new(2026, 10, 9);

        MealOrder unpaid = TestDataFactory.CreateCompletedOrder(user.Id, meal.Id, endDate, 10m);

        MealOrder paid = TestDataFactory.CreateCompletedOrder(user.Id, meal.Id, startDate, 20m);
        paid.MarkAsPaid(DateTime.Now, "ADMIN");

        MealOrder deleted = TestDataFactory.CreateOrder(user.Id, meal.Id, startDate, 30m);
        deleted.SoftDelete();

        MealOrder cancelled = TestDataFactory.CreateOrder(user.Id, meal.Id, startDate, 40m);
        cancelled.Cancel();

        MealOrder beforePeriod = TestDataFactory.CreateCompletedOrder(user.Id, meal.Id, startDate.AddDays(-1), 50m);
        MealOrder afterPeriod = TestDataFactory.CreateCompletedOrder(user.Id, meal.Id, endDate.AddDays(1), 60m);
        MealOrder pendingInPeriod = TestDataFactory.CreateOrder(user.Id, meal.Id, endDate, 70m);

        await _context.MealOrders.AddRangeAsync(unpaid, paid, deleted, cancelled, beforePeriod, afterPeriod, pendingInPeriod);
        await _context.SaveChangesAsync();

        IReadOnlyList<EmployeeOutstandingSummary> result =
            await _repository.GetOutstandingSummariesAsync(startDate, endDate, 0m);

        EmployeeOutstandingSummary summary = Assert.Single(result);
        Assert.Equal(1, summary.UnpaidCount);
        Assert.Equal(10m, summary.GrossAmount);
        Assert.Equal(0m, summary.PortionAmount);
        Assert.Equal(10m, summary.NetAmount);
        Assert.Equal(1, summary.EarlierUnpaidCount);
        Assert.Equal(50m, summary.EarlierNetAmount);
        Assert.Equal(60m, summary.TotalDue);
    }

    [Fact]
    public async Task GetOutstandingSummariesAsync_WhenDayAlreadyHasPortionOnPaidOrder_ShouldNotAddAnotherPortion()
    {
        (ApplicationUser user, Meal meal, Menu _) = await SeedUserMealMenuAsync();
        DateTime menuDate = new(2026, 10, 5);

        MealOrder paidWithPortion = TestDataFactory.CreateCompletedOrder(user.Id, meal.Id, menuDate, 10m);
        paidWithPortion.ApplyPortion(3m);
        paidWithPortion.MarkAsPaid(DateTime.Now, "ADMIN");

        MealOrder unpaid = TestDataFactory.CreateCompletedOrder(user.Id, meal.Id, menuDate, 7m);

        await _context.MealOrders.AddRangeAsync(paidWithPortion, unpaid);
        await _context.SaveChangesAsync();

        IReadOnlyList<EmployeeOutstandingSummary> result =
            await _repository.GetOutstandingSummariesAsync(menuDate, menuDate, 3m);

        EmployeeOutstandingSummary summary = Assert.Single(result);
        Assert.Equal(1, summary.UnpaidCount);
        Assert.Equal(7m, summary.GrossAmount);
        Assert.Equal(0m, summary.PortionAmount);
        Assert.Equal(7m, summary.NetAmount);
    }

    [Fact]
    public async Task GetOutstandingSummariesAsync_WhenPortionExceedsPrice_ShouldCapPortionAtPrice()
    {
        (ApplicationUser user, Meal meal, Menu _) = await SeedUserMealMenuAsync();
        DateTime menuDate = new(2026, 10, 5);

        MealOrder cheapOrder = TestDataFactory.CreateCompletedOrder(user.Id, meal.Id, menuDate, 2m);

        await _context.MealOrders.AddAsync(cheapOrder);
        await _context.SaveChangesAsync();

        IReadOnlyList<EmployeeOutstandingSummary> result =
            await _repository.GetOutstandingSummariesAsync(menuDate, menuDate, 3m);

        EmployeeOutstandingSummary summary = Assert.Single(result);
        Assert.Equal(2m, summary.PortionAmount);
        Assert.Equal(0m, summary.NetAmount);
    }

    [Fact]
    public async Task GetOutstandingSummariesAsync_WhenNoUnpaidOrdersUpToEndDate_ShouldReturnEmpty()
    {
        (ApplicationUser user, Meal meal, Menu _) = await SeedUserMealMenuAsync();
        MealOrder paid = TestDataFactory.CreateCompletedOrder(user.Id, meal.Id, new DateTime(2026, 9, 1), 10m);
        paid.MarkAsPaid(DateTime.Now, "ADMIN");
        MealOrder afterPeriod = TestDataFactory.CreateCompletedOrder(user.Id, meal.Id, new DateTime(2026, 11, 3), 10m);

        await _context.MealOrders.AddRangeAsync(paid, afterPeriod);
        await _context.SaveChangesAsync();

        IReadOnlyList<EmployeeOutstandingSummary> result =
            await _repository.GetOutstandingSummariesAsync(new DateTime(2026, 10, 1), new DateTime(2026, 10, 31), 3m);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetOutstandingSummariesAsync_WhenEmployeeHasOnlyEarlierDebt_ShouldListThemWithEmptyPeriod()
    {
        (ApplicationUser user, Meal meal, Menu _) = await SeedUserMealMenuAsync();
        MealOrder earlier = TestDataFactory.CreateCompletedOrder(user.Id, meal.Id, new DateTime(2026, 9, 1), 10m);

        await _context.MealOrders.AddAsync(earlier);
        await _context.SaveChangesAsync();

        IReadOnlyList<EmployeeOutstandingSummary> result =
            await _repository.GetOutstandingSummariesAsync(new DateTime(2026, 10, 1), new DateTime(2026, 10, 31), 3m);

        EmployeeOutstandingSummary summary = Assert.Single(result);
        Assert.Equal(0, summary.UnpaidCount);
        Assert.Equal(0m, summary.NetAmount);
        Assert.Equal(1, summary.EarlierUnpaidCount);
        Assert.Equal(7m, summary.EarlierNetAmount);
        Assert.Equal(7m, summary.TotalDue);
    }

    [Fact]
    public async Task GetOutstandingSummariesAsync_WhenUserIdIsProvided_ShouldReturnOnlyThatEmployee()
    {
        (ApplicationUser user, Meal meal, Menu _) = await SeedUserMealMenuAsync();
        ApplicationUser other = await SeedUserAsync("OTH", "Other User");
        DateTime day = new(2026, 10, 5);

        await _context.MealOrders.AddRangeAsync(
            TestDataFactory.CreateCompletedOrder(user.Id, meal.Id, day, 10m),
            TestDataFactory.CreateCompletedOrder(other.Id, meal.Id, day, 20m));
        await _context.SaveChangesAsync();

        IReadOnlyList<EmployeeOutstandingSummary> result =
            await _repository.GetOutstandingSummariesAsync(day, day, 0m, other.Id);

        EmployeeOutstandingSummary summary = Assert.Single(result);
        Assert.Equal(other.Id, summary.UserId);
        Assert.Equal(20m, summary.NetAmount);
    }

    [Fact]
    public async Task GetOutstandingSummariesAsync_WhenDatesAreNull_ShouldIncludeAllUnpaidCompletedOrders()
    {
        (ApplicationUser user, Meal meal, Menu _) = await SeedUserMealMenuAsync();

        await _context.MealOrders.AddRangeAsync(
            TestDataFactory.CreateCompletedOrder(user.Id, meal.Id, new DateTime(2025, 1, 10), 10m),
            TestDataFactory.CreateCompletedOrder(user.Id, meal.Id, DateTime.Today.AddDays(30), 20m),
            TestDataFactory.CreateOrder(user.Id, meal.Id, DateTime.Today, 30m));
        await _context.SaveChangesAsync();

        IReadOnlyList<EmployeeOutstandingSummary> result =
            await _repository.GetOutstandingSummariesAsync(null, null, 0m);

        EmployeeOutstandingSummary summary = Assert.Single(result);
        Assert.Equal(2, summary.UnpaidCount);
        Assert.Equal(30m, summary.NetAmount);
        Assert.Equal(0, summary.EarlierUnpaidCount);
    }

    [Fact]
    public async Task MarkAllUnpaidAsPaidAsync_WhenDateIsNull_ShouldPayEveryUnpaidCompletedOrder()
    {
        (ApplicationUser user, Meal meal, Menu _) = await SeedUserMealMenuAsync();
        MealOrder old = TestDataFactory.CreateCompletedOrder(user.Id, meal.Id, new DateTime(2025, 1, 10), 10m);
        MealOrder future = TestDataFactory.CreateCompletedOrder(user.Id, meal.Id, DateTime.Today.AddDays(30), 20m);
        MealOrder pending = TestDataFactory.CreateOrder(user.Id, meal.Id, DateTime.Today, 30m);

        await _context.MealOrders.AddRangeAsync(old, future, pending);
        await _context.SaveChangesAsync();

        (int paidCount, decimal totalPaid) result = await _repository.MarkAllUnpaidAsPaidAsync(
            user.Id,
            null,
            DateTime.Now,
            0m,
            "ADMIN");

        Assert.Equal(2, result.paidCount);
        Assert.Equal(30m, result.totalPaid);
        MealOrder persistedPending = await _context.MealOrders.FirstAsync(x => x.Id == pending.Id);
        Assert.Equal(PaymentStatus.Unpaid, persistedPending.PaymentStatus);
    }

    [Fact]
    public async Task GetExpiredMenuIdsWithPendingOrdersAsync_WhenMenuHasExpired_ShouldReturnItOnlyIfItHasPendingOrders()
    {
        (ApplicationUser user, Meal meal, Menu expiredWithPending) = await SeedUserMealMenuAsync();

        Supplier secondSupplier = TestDataFactory.CreateSupplier();
        Menu expiredCompletedOnly = TestDataFactory.CreateMenu(secondSupplier.Id);
        Meal completedMeal = expiredCompletedOnly.Meals.Single();

        Supplier thirdSupplier = TestDataFactory.CreateSupplier();
        Menu stillOpen = TestDataFactory.CreateMenu(thirdSupplier.Id, activeUntil: DateTime.Today.AddDays(2).AddHours(12));
        Meal openMeal = stillOpen.Meals.Single();

        await _context.Suppliers.AddRangeAsync(secondSupplier, thirdSupplier);
        await _context.Menus.AddRangeAsync(expiredCompletedOnly, stillOpen);
        await _context.SaveChangesAsync();

        MealOrder deletedPending = TestDataFactory.CreateOrder(user.Id, meal.Id, expiredWithPending.Date, 5m);
        deletedPending.SoftDelete();

        await _context.MealOrders.AddRangeAsync(
            TestDataFactory.CreateOrder(user.Id, meal.Id, expiredWithPending.Date, 10m),
            deletedPending,
            TestDataFactory.CreateCompletedOrder(user.Id, completedMeal.Id, expiredCompletedOnly.Date, 11m),
            TestDataFactory.CreateOrder(user.Id, openMeal.Id, stillOpen.Date, 12m));
        await _context.SaveChangesAsync();

        // Default test menus close tomorrow at 12:00, the third one a day later.
        DateTime now = DateTime.Today.AddDays(1).AddHours(13);

        IReadOnlyList<Guid> result = await _repository.GetExpiredMenuIdsWithPendingOrdersAsync(now);

        Assert.Equal([expiredWithPending.Id], result);
    }

    [Fact]
    public async Task GetExpiredMenuIdsWithPendingOrdersAsync_WhenMenuIsStillOpen_ShouldReturnEmpty()
    {
        (ApplicationUser user, Meal meal, Menu menu) = await SeedUserMealMenuAsync();
        await _context.MealOrders.AddAsync(TestDataFactory.CreateOrder(user.Id, meal.Id, menu.Date, 10m));
        await _context.SaveChangesAsync();

        IReadOnlyList<Guid> result = await _repository.GetExpiredMenuIdsWithPendingOrdersAsync(menu.ActiveUntil.AddMinutes(-1));

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUserOrderItemsAsync_WhenFilteringByDate_ShouldUseMenuDateNotOrderedAt()
    {
        (ApplicationUser user, Meal meal, Menu menu) = await SeedUserMealMenuAsync();
        MealOrder order = TestDataFactory.CreateOrder(user.Id, meal.Id, menu.Date, 10m);
        await _context.MealOrders.AddAsync(order);
        await _context.SaveChangesAsync();

        IReadOnlyList<UserOrderItem> orderedToday = await _repository.GetUserOrderItemsAsync(user.Id, startDate: DateTime.Today, endDate: DateTime.Today);
        IReadOnlyList<UserOrderItem> forMenuDay = await _repository.GetUserOrderItemsAsync(user.Id, startDate: menu.Date, endDate: menu.Date);

        Assert.Empty(orderedToday);
        Assert.Equal(order.Id, Assert.Single(forMenuDay).OrderId);
    }

    [Fact]
    public async Task GetUserOrderItemsAsync_WhenOrderIsPaid_ShouldReturnPaidFlagAndDate()
    {
        (ApplicationUser user, Meal meal, Menu menu) = await SeedUserMealMenuAsync();
        DateTime paidOn = DateTime.Today.AddHours(15);
        MealOrder paid = TestDataFactory.CreateCompletedOrder(user.Id, meal.Id, menu.Date, 7m);
        paid.MarkAsPaid(paidOn, "admin-id");
        MealOrder unpaid = TestDataFactory.CreateOrder(user.Id, meal.Id, menu.Date, 7m);

        await _context.MealOrders.AddRangeAsync(paid, unpaid);
        await _context.SaveChangesAsync();

        IReadOnlyList<UserOrderItem> result = await _repository.GetUserOrderItemsAsync(user.Id);

        UserOrderItem paidItem = result.Single(x => x.OrderId == paid.Id);
        UserOrderItem unpaidItem = result.Single(x => x.OrderId == unpaid.Id);
        Assert.True(paidItem.IsPaid);
        Assert.Equal(paidOn, paidItem.PaidOn);
        Assert.False(unpaidItem.IsPaid);
        Assert.Null(unpaidItem.PaidOn);
    }

    [Fact]
    public async Task GetExpectedPortionsAsync_WhenDayHasUnpaidOrdersFromSeveralSuppliers_ShouldGiveOnePortionToTheMostExpensive()
    {
        (ApplicationUser user, Meal meal, Menu menu) = await SeedUserMealMenuAsync();

        Supplier otherSupplier = TestDataFactory.CreateSupplier(name: "Other");
        Menu otherMenu = TestDataFactory.CreateMenu(otherSupplier.Id, date: menu.Date);
        Meal otherMeal = otherMenu.Meals.Single();
        await _context.Suppliers.AddAsync(otherSupplier);
        await _context.Menus.AddAsync(otherMenu);
        await _context.SaveChangesAsync();

        MealOrder cheap = TestDataFactory.CreateOrder(user.Id, meal.Id, menu.Date, 3m);
        MealOrder secondCheap = TestDataFactory.CreateOrder(user.Id, meal.Id, menu.Date, 3m);
        MealOrder expensive = TestDataFactory.CreateOrder(user.Id, otherMeal.Id, menu.Date, 8m);
        MealOrder cancelled = TestDataFactory.CreateOrder(user.Id, otherMeal.Id, menu.Date, 20m);
        cancelled.Cancel();

        await _context.MealOrders.AddRangeAsync(cheap, secondCheap, expensive, cancelled);
        await _context.SaveChangesAsync();

        IReadOnlyDictionary<Guid, decimal> result = await _repository.GetExpectedPortionsAsync(user.Id, [menu.Date], 2.5m);

        KeyValuePair<Guid, decimal> portion = Assert.Single(result);
        Assert.Equal(expensive.Id, portion.Key);
        Assert.Equal(2.5m, portion.Value);
    }

    [Fact]
    public async Task GetExpectedPortionsAsync_WhenDayAlreadyHasPortionApplied_ShouldReturnEmpty()
    {
        (ApplicationUser user, Meal meal, Menu menu) = await SeedUserMealMenuAsync();

        MealOrder paid = TestDataFactory.CreateCompletedOrder(user.Id, meal.Id, menu.Date, 5m);
        paid.ApplyPortion(2m);
        paid.MarkAsPaid(DateTime.Now, "admin-id");
        MealOrder unpaid = TestDataFactory.CreateOrder(user.Id, meal.Id, menu.Date, 9m);

        await _context.MealOrders.AddRangeAsync(paid, unpaid);
        await _context.SaveChangesAsync();

        IReadOnlyDictionary<Guid, decimal> result = await _repository.GetExpectedPortionsAsync(user.Id, [menu.Date], 2m);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetExpectedPortionsAsync_WhenPortionIsLargerThanPrice_ShouldCapAtPrice()
    {
        (ApplicationUser user, Meal meal, Menu menu) = await SeedUserMealMenuAsync();
        MealOrder order = TestDataFactory.CreateOrder(user.Id, meal.Id, menu.Date, 1.5m);
        await _context.MealOrders.AddAsync(order);
        await _context.SaveChangesAsync();

        IReadOnlyDictionary<Guid, decimal> result = await _repository.GetExpectedPortionsAsync(user.Id, [menu.Date], 2m);

        Assert.Equal(1.5m, result[order.Id]);
    }

    [Fact]
    public async Task GetExpectedPortionsAsync_WhenCompanyPortionIsZero_ShouldReturnEmpty()
    {
        (ApplicationUser user, Meal meal, Menu menu) = await SeedUserMealMenuAsync();
        await _context.MealOrders.AddAsync(TestDataFactory.CreateOrder(user.Id, meal.Id, menu.Date, 10m));
        await _context.SaveChangesAsync();

        IReadOnlyDictionary<Guid, decimal> result = await _repository.GetExpectedPortionsAsync(user.Id, [menu.Date], 0m);

        Assert.Empty(result);
    }

    private async Task<ApplicationUser> SeedUserAsync(string code, string fullName)
    {
        ApplicationUser user = TestDataFactory.CreateUser(Guid.NewGuid().ToString(), code, fullName);

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        return user;
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
