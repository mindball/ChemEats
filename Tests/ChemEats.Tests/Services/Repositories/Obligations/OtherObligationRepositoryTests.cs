using ChemEats.Tests.TestInfrastructure;
using Domain;
using Domain.Entities;
using Domain.Infrastructure.Exceptions;
using Domain.Infrastructure.Identity;
using Domain.Models.Obligations;
using Microsoft.EntityFrameworkCore;
using Services.Repositories.Obligations;

namespace ChemEats.Tests.Services.Repositories.Obligations;

public sealed class OtherObligationRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly OtherObligationRepository _repository;

    public OtherObligationRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _repository = new OtherObligationRepository(_context);
    }

    [Fact]
    public async Task AddAsync_WhenUserExists_ShouldPersistAndReturnItemWithNames()
    {
        await SeedUsersAsync();
        OtherObligation obligation = TestDataFactory.CreateObligation("user-1", 15m, new DateTime(2026, 10, 5), createdByUserId: "admin-id");

        await _repository.AddAsync(obligation);
        OtherObligationItem? item = await _repository.GetItemByIdAsync(obligation.Id);

        Assert.NotNull(item);
        Assert.Equal("User One", item.EmployeeName);
        Assert.Equal("Admin User", item.CreatedByName);
        Assert.Equal(15m, item.Amount);
        Assert.Equal(PaymentStatus.Unpaid, item.PaymentStatus);
        Assert.Null(item.PaidByName);
    }

    [Fact]
    public async Task AddAsync_WhenUserDoesNotExist_ShouldThrowArgumentException()
    {
        await SeedUsersAsync();
        OtherObligation obligation = TestDataFactory.CreateObligation("missing-user");

        await Assert.ThrowsAsync<ArgumentException>(() => _repository.AddAsync(obligation));
    }

    [Fact]
    public async Task GetForUserAsync_ShouldReturnOnlyThatUsersObligationsInRangeOrderedByDate()
    {
        await SeedUsersAsync();
        await SeedObligationsAsync(
            TestDataFactory.CreateObligation("user-1", 10m, new DateTime(2026, 10, 20)),
            TestDataFactory.CreateObligation("user-1", 20m, new DateTime(2026, 10, 2)),
            TestDataFactory.CreateObligation("user-1", 30m, new DateTime(2026, 9, 30)),
            TestDataFactory.CreateObligation("user-2", 40m, new DateTime(2026, 10, 10)));

        IReadOnlyList<OtherObligationItem> result = await _repository.GetForUserAsync(
            "user-1",
            new DateTime(2026, 10, 1),
            new DateTime(2026, 10, 31));

        Assert.Equal([20m, 10m], result.Select(x => x.Amount));
        Assert.All(result, x => Assert.Equal("user-1", x.UserId));
    }

    [Fact]
    public async Task GetForUserAsync_WhenObligationIsDeleted_ShouldExcludeIt()
    {
        await SeedUsersAsync();
        OtherObligation kept = TestDataFactory.CreateObligation("user-1", 10m);
        OtherObligation deleted = TestDataFactory.CreateObligation("user-1", 20m);
        await SeedObligationsAsync(kept, deleted);

        await _repository.SoftDeleteAsync(deleted.Id);

        IReadOnlyList<OtherObligationItem> result = await _repository.GetForUserAsync("user-1");
        Assert.Equal(kept.Id, Assert.Single(result).Id);
    }

    [Fact]
    public async Task UpdateAsync_WhenUnpaid_ShouldPersistChanges()
    {
        await SeedUsersAsync();
        OtherObligation obligation = TestDataFactory.CreateObligation("user-1", 10m);
        await SeedObligationsAsync(obligation);

        bool updated = await _repository.UpdateAsync(obligation.Id, "Keyboard", 35.9m, new DateTime(2026, 10, 7));

        Assert.True(updated);
        OtherObligationItem? item = await _repository.GetItemByIdAsync(obligation.Id);
        Assert.NotNull(item);
        Assert.Equal("Keyboard", item.Description);
        Assert.Equal(35.9m, item.Amount);
        Assert.Equal(new DateTime(2026, 10, 7), item.Date);
    }

    [Fact]
    public async Task UpdateAsync_WhenObligationDoesNotExist_ShouldReturnFalse()
    {
        bool updated = await _repository.UpdateAsync(Guid.NewGuid(), "Keyboard", 10m, DateTime.Today);

        Assert.False(updated);
    }

    [Fact]
    public async Task UpdateAsync_WhenPaid_ShouldThrowDomainException()
    {
        await SeedUsersAsync();
        OtherObligation obligation = TestDataFactory.CreateObligation("user-1", 10m);
        await SeedObligationsAsync(obligation);
        await _repository.MarkAsPaidAsync("user-1", [obligation.Id], DateTime.Now, "admin-id");

        await Assert.ThrowsAsync<DomainException>(() =>
            _repository.UpdateAsync(obligation.Id, "Keyboard", 10m, DateTime.Today));
    }

    [Fact]
    public async Task SoftDeleteAsync_WhenObligationDoesNotExist_ShouldReturnFalse()
    {
        bool deleted = await _repository.SoftDeleteAsync(Guid.NewGuid());

        Assert.False(deleted);
    }

    [Fact]
    public async Task MarkAsPaidAsync_ShouldPayOnlyUnpaidObligationsOfThatUser()
    {
        await SeedUsersAsync();
        OtherObligation first = TestDataFactory.CreateObligation("user-1", 10m);
        OtherObligation second = TestDataFactory.CreateObligation("user-1", 5.5m);
        OtherObligation otherUsers = TestDataFactory.CreateObligation("user-2", 100m);
        await SeedObligationsAsync(first, second, otherUsers);

        (int paidCount, decimal totalPaid) = await _repository.MarkAsPaidAsync(
            "user-1",
            [first.Id, second.Id, otherUsers.Id],
            new DateTime(2026, 11, 3, 10, 0, 0),
            "admin-id");

        Assert.Equal(2, paidCount);
        Assert.Equal(15.5m, totalPaid);

        OtherObligationItem? paid = await _repository.GetItemByIdAsync(first.Id);
        Assert.Equal(PaymentStatus.Paid, paid!.PaymentStatus);
        Assert.Equal("Admin User", paid.PaidByName);

        OtherObligationItem? untouched = await _repository.GetItemByIdAsync(otherUsers.Id);
        Assert.Equal(PaymentStatus.Unpaid, untouched!.PaymentStatus);
    }

    [Fact]
    public async Task MarkAsPaidAsync_WhenAlreadyPaid_ShouldSkipIt()
    {
        await SeedUsersAsync();
        OtherObligation obligation = TestDataFactory.CreateObligation("user-1", 10m);
        await SeedObligationsAsync(obligation);
        await _repository.MarkAsPaidAsync("user-1", [obligation.Id], DateTime.Now, "admin-id");

        (int paidCount, decimal totalPaid) = await _repository.MarkAsPaidAsync("user-1", [obligation.Id], DateTime.Now, "admin-id");

        Assert.Equal(0, paidCount);
        Assert.Equal(0m, totalPaid);
    }

    [Fact]
    public async Task MarkAllUnpaidAsPaidAsync_WhenUpToDateIsSet_ShouldPayOnlyObligationsUpToThatDate()
    {
        await SeedUsersAsync();
        await SeedObligationsAsync(
            TestDataFactory.CreateObligation("user-1", 10m, new DateTime(2026, 9, 15)),
            TestDataFactory.CreateObligation("user-1", 20m, new DateTime(2026, 10, 31)),
            TestDataFactory.CreateObligation("user-1", 40m, new DateTime(2026, 11, 1)));

        (int paidCount, decimal totalPaid) = await _repository.MarkAllUnpaidAsPaidAsync(
            "user-1",
            new DateTime(2026, 10, 31),
            DateTime.Now,
            "admin-id");

        Assert.Equal(2, paidCount);
        Assert.Equal(30m, totalPaid);

        IReadOnlyList<OtherObligationItem> unpaid = await _repository.GetForUserAsync("user-1", paymentStatus: PaymentStatus.Unpaid);
        Assert.Equal(40m, Assert.Single(unpaid).Amount);
    }

    [Fact]
    public async Task MarkAllUnpaidAsPaidAsync_WhenUpToDateIsNull_ShouldPayEverything()
    {
        await SeedUsersAsync();
        await SeedObligationsAsync(
            TestDataFactory.CreateObligation("user-1", 10m, new DateTime(2026, 9, 15)),
            TestDataFactory.CreateObligation("user-1", 40m, DateTime.Today.AddDays(10)));

        (int paidCount, decimal totalPaid) = await _repository.MarkAllUnpaidAsPaidAsync("user-1", null, DateTime.Now, "admin-id");

        Assert.Equal(2, paidCount);
        Assert.Equal(50m, totalPaid);
    }

    [Fact]
    public async Task GetOutstandingSummariesAsync_ShouldSumUnpaidUpToDatePerEmployeeExcludingPaidAndDeleted()
    {
        await SeedUsersAsync();
        OtherObligation paid = TestDataFactory.CreateObligation("user-1", 99m, new DateTime(2026, 10, 3));
        OtherObligation deleted = TestDataFactory.CreateObligation("user-1", 77m, new DateTime(2026, 10, 3));
        await SeedObligationsAsync(
            TestDataFactory.CreateObligation("user-1", 10m, new DateTime(2026, 8, 1)),
            TestDataFactory.CreateObligation("user-1", 5.25m, new DateTime(2026, 10, 31)),
            TestDataFactory.CreateObligation("user-1", 1000m, new DateTime(2026, 11, 1)),
            TestDataFactory.CreateObligation("user-2", 7m, new DateTime(2026, 10, 10)),
            paid,
            deleted);
        await _repository.MarkAsPaidAsync("user-1", [paid.Id], DateTime.Now, "admin-id");
        await _repository.SoftDeleteAsync(deleted.Id);

        IReadOnlyList<EmployeeObligationSummary> result = await _repository.GetOutstandingSummariesAsync(new DateTime(2026, 10, 31));

        Assert.Equal(2, result.Count);
        EmployeeObligationSummary userOne = Assert.Single(result, s => s.UserId == "user-1");
        Assert.Equal(2, userOne.UnpaidCount);
        Assert.Equal(15.25m, userOne.Amount);
        Assert.Equal("User One", userOne.EmployeeName);
        Assert.Equal(7m, Assert.Single(result, s => s.UserId == "user-2").Amount);
    }

    [Fact]
    public async Task GetOutstandingSummariesAsync_WhenUserIdIsSet_ShouldReturnOnlyThatUser()
    {
        await SeedUsersAsync();
        await SeedObligationsAsync(
            TestDataFactory.CreateObligation("user-1", 10m),
            TestDataFactory.CreateObligation("user-2", 7m));

        IReadOnlyList<EmployeeObligationSummary> result = await _repository.GetOutstandingSummariesAsync(null, "user-2");

        Assert.Equal("user-2", Assert.Single(result).UserId);
    }

    private async Task SeedUsersAsync()
    {
        _context.Users.AddRange(
            TestDataFactory.CreateUser("user-1", "U1", "User One"),
            TestDataFactory.CreateUser("user-2", "U2", "User Two"),
            TestDataFactory.CreateUser("admin-id", "AD", "Admin User"));
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    private async Task SeedObligationsAsync(params OtherObligation[] obligations)
    {
        _context.OtherObligations.AddRange(obligations);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }
}
