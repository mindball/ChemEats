using ChemEats.Tests.TestInfrastructure;
using Domain;
using Domain.Entities;
using Domain.Infrastructure.Identity;
using Domain.Models.Notifications;
using Microsoft.EntityFrameworkCore;
using Services.Repositories.Notifications;

namespace ChemEats.Tests.Services.Repositories.Notifications;

public sealed class OrderReminderRepositoryTests : IDisposable
{
    private static readonly TimeSpan LeadTime = TimeSpan.FromHours(2);

    private readonly AppDbContext _context;
    private readonly OrderReminderRepository _repository;

    public OrderReminderRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _repository = new OrderReminderRepository(_context);
    }

    [Fact]
    public async Task GetDueRemindersAsync_WhenOptedInUserHasNoOrder_ShouldReturnCandidate()
    {
        (Menu menu, Meal _) = await SeedMenuAsync("Supplier A");
        ApplicationUser user = await SeedUserAsync("AA", optedIn: true, "aa.personal@example.com");

        List<OrderReminderCandidate> result = await _repository.GetDueRemindersAsync(menu.ActiveUntil.AddHours(-1), LeadTime);

        OrderReminderCandidate candidate = Assert.Single(result);
        Assert.Equal(user.Id, candidate.UserId);
        Assert.Equal("aa.personal@example.com", candidate.NotificationEmail);
        Assert.Equal(menu.Id, candidate.MenuId);
        Assert.Equal("Supplier A", candidate.SupplierName);
        Assert.Equal(menu.ActiveUntil, candidate.ActiveUntil);
    }

    [Fact]
    public async Task GetDueRemindersAsync_WhenUserOptedOut_ShouldReturnEmpty()
    {
        (Menu menu, Meal _) = await SeedMenuAsync();
        await SeedUserAsync("AA", optedIn: false, "aa.personal@example.com");

        List<OrderReminderCandidate> result = await _repository.GetDueRemindersAsync(menu.ActiveUntil.AddHours(-1), LeadTime);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDueRemindersAsync_WhenUserAlreadyOrderedForMenuDate_ShouldReturnEmpty()
    {
        (Menu menu, Meal meal) = await SeedMenuAsync();
        ApplicationUser user = await SeedUserAsync("AA", optedIn: true, "aa.personal@example.com");
        await SeedOrderAsync(user.Id, meal.Id, menu.Date);

        List<OrderReminderCandidate> result = await _repository.GetDueRemindersAsync(menu.ActiveUntil.AddHours(-1), LeadTime);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDueRemindersAsync_WhenUserOrderedFromAnotherSupplierSameDate_ShouldReturnEmpty()
    {
        (Menu menuA, Meal _) = await SeedMenuAsync("Supplier A");
        (Menu _, Meal mealB) = await SeedMenuAsync("Supplier B");
        ApplicationUser user = await SeedUserAsync("AA", optedIn: true, "aa.personal@example.com");
        await SeedOrderAsync(user.Id, mealB.Id, menuA.Date);

        List<OrderReminderCandidate> result = await _repository.GetDueRemindersAsync(menuA.ActiveUntil.AddHours(-1), LeadTime);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDueRemindersAsync_WhenOnlyOrderIsDeleted_ShouldReturnCandidate()
    {
        (Menu menu, Meal meal) = await SeedMenuAsync();
        ApplicationUser user = await SeedUserAsync("AA", optedIn: true, "aa.personal@example.com");
        await SeedOrderAsync(user.Id, meal.Id, menu.Date, order => order.SoftDelete());

        List<OrderReminderCandidate> result = await _repository.GetDueRemindersAsync(menu.ActiveUntil.AddHours(-1), LeadTime);

        Assert.Equal(user.Id, Assert.Single(result).UserId);
    }

    [Fact]
    public async Task GetDueRemindersAsync_WhenOnlyOrderIsCancelled_ShouldReturnCandidate()
    {
        (Menu menu, Meal meal) = await SeedMenuAsync();
        ApplicationUser user = await SeedUserAsync("AA", optedIn: true, "aa.personal@example.com");
        await SeedOrderAsync(user.Id, meal.Id, menu.Date, order => order.Cancel());

        List<OrderReminderCandidate> result = await _repository.GetDueRemindersAsync(menu.ActiveUntil.AddHours(-1), LeadTime);

        Assert.Equal(user.Id, Assert.Single(result).UserId);
    }

    [Fact]
    public async Task GetDueRemindersAsync_WhenUserOrderedForDifferentDate_ShouldReturnCandidate()
    {
        (Menu menu, Meal meal) = await SeedMenuAsync();
        ApplicationUser user = await SeedUserAsync("AA", optedIn: true, "aa.personal@example.com");
        await SeedOrderAsync(user.Id, meal.Id, menu.Date.AddDays(1));

        List<OrderReminderCandidate> result = await _repository.GetDueRemindersAsync(menu.ActiveUntil.AddHours(-1), LeadTime);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetDueRemindersAsync_WhenReminderAlreadySent_ShouldReturnEmpty()
    {
        (Menu menu, Meal _) = await SeedMenuAsync();
        ApplicationUser user = await SeedUserAsync("AA", optedIn: true, "aa.personal@example.com");
        await _repository.AddReminderLogsAsync([MenuReminderLog.Create(user.Id, menu.Id, DateTime.Now)]);

        List<OrderReminderCandidate> result = await _repository.GetDueRemindersAsync(menu.ActiveUntil.AddHours(-1), LeadTime);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDueRemindersAsync_WhenDeadlineIsBeyondLeadTime_ShouldReturnEmpty()
    {
        (Menu menu, Meal _) = await SeedMenuAsync();
        await SeedUserAsync("AA", optedIn: true, "aa.personal@example.com");

        List<OrderReminderCandidate> result = await _repository.GetDueRemindersAsync(menu.ActiveUntil.AddHours(-3), LeadTime);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDueRemindersAsync_WhenDeadlineHasPassed_ShouldReturnEmpty()
    {
        (Menu menu, Meal _) = await SeedMenuAsync();
        await SeedUserAsync("AA", optedIn: true, "aa.personal@example.com");

        List<OrderReminderCandidate> result = await _repository.GetDueRemindersAsync(menu.ActiveUntil.AddMinutes(1), LeadTime);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDueRemindersAsync_WhenMenuIsDeleted_ShouldReturnEmpty()
    {
        (Menu menu, Meal _) = await SeedMenuAsync();
        await SeedUserAsync("AA", optedIn: true, "aa.personal@example.com");
        menu.SoftDeleteWithPendingOrders([]);
        await _context.SaveChangesAsync();

        List<OrderReminderCandidate> result = await _repository.GetDueRemindersAsync(menu.ActiveUntil.AddHours(-1), LeadTime);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDueRemindersAsync_WithMixedUsers_ShouldReturnOnlyThoseNeedingReminder()
    {
        (Menu menu, Meal meal) = await SeedMenuAsync();
        ApplicationUser remind = await SeedUserAsync("AA", optedIn: true, "aa@example.com");
        ApplicationUser ordered = await SeedUserAsync("BB", optedIn: true, "bb@example.com");
        await SeedUserAsync("CC", optedIn: false, null);
        await SeedOrderAsync(ordered.Id, meal.Id, menu.Date);

        List<OrderReminderCandidate> result = await _repository.GetDueRemindersAsync(menu.ActiveUntil.AddHours(-1), LeadTime);

        Assert.Equal(remind.Id, Assert.Single(result).UserId);
    }

    [Fact]
    public async Task AddReminderLogsAsync_ShouldPersistLogs()
    {
        (Menu menu, Meal _) = await SeedMenuAsync();
        ApplicationUser user = await SeedUserAsync("AA", optedIn: true, "aa@example.com");

        await _repository.AddReminderLogsAsync([MenuReminderLog.Create(user.Id, menu.Id, DateTime.Now)]);

        Assert.Equal(1, await _context.MenuReminderLogs.CountAsync());
    }

    [Fact]
    public async Task AddReminderLogsAsync_WhenDuplicateUserAndMenu_ShouldThrowAndClearTracking()
    {
        (Menu menu, Meal _) = await SeedMenuAsync();
        ApplicationUser user = await SeedUserAsync("AA", optedIn: true, "aa@example.com");
        await _repository.AddReminderLogsAsync([MenuReminderLog.Create(user.Id, menu.Id, DateTime.Now)]);

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            _repository.AddReminderLogsAsync([MenuReminderLog.Create(user.Id, menu.Id, DateTime.Now)]));

        Assert.Empty(_context.ChangeTracker.Entries<MenuReminderLog>().Where(e => e.State == EntityState.Added));
    }

    [Fact]
    public async Task AddReminderLogsAsync_WhenLogsAreNull_ShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.AddReminderLogsAsync(null!));
    }

    private async Task<(Menu Menu, Meal Meal)> SeedMenuAsync(string supplierName = "Supplier")
    {
        Supplier supplier = TestDataFactory.CreateSupplier(name: supplierName);
        Menu menu = TestDataFactory.CreateMenu(supplier.Id);

        await _context.Suppliers.AddAsync(supplier);
        await _context.Menus.AddAsync(menu);
        await _context.SaveChangesAsync();

        return (menu, menu.Meals.Single());
    }

    private async Task<ApplicationUser> SeedUserAsync(string code, bool optedIn, string? notificationEmail)
    {
        ApplicationUser user = TestDataFactory.CreateUser(Guid.NewGuid().ToString(), code, $"User {code}");
        user.UpdateNotificationSettings(notificationEmail, optedIn);

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        return user;
    }

    private async Task SeedOrderAsync(string userId, Guid mealId, DateTime menuDate, Action<MealOrder>? configure = null)
    {
        MealOrder order = TestDataFactory.CreateOrder(userId, mealId, menuDate, 10m);
        configure?.Invoke(order);

        await _context.MealOrders.AddAsync(order);
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
