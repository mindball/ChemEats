using ChemEats.Tests.TestInfrastructure;
using Domain;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Services.Repositories.Obligations;

namespace ChemEats.Tests.Services.Repositories.Obligations;

public sealed class ObligationTypeRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ObligationTypeRepository _repository;

    public ObligationTypeRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _repository = new ObligationTypeRepository(_context);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnActiveTypesSortedByName()
    {
        ObligationType deleted = TestDataFactory.CreateObligationType("Archived");
        await SeedAsync(
            TestDataFactory.CreateObligationType("Toner"),
            TestDataFactory.CreateObligationType("keyboard"),
            deleted);
        await _repository.SoftDeleteAsync(deleted.Id);

        IReadOnlyList<ObligationType> result = await _repository.GetAllAsync();

        Assert.Equal(["keyboard", "Toner"], result.Select(t => t.Name));
    }

    [Fact]
    public async Task GetByIdAsync_WhenTypeIsDeleted_ShouldReturnNull()
    {
        ObligationType type = TestDataFactory.CreateObligationType("Toner");
        await SeedAsync(type);
        await _repository.SoftDeleteAsync(type.Id);

        Assert.Null(await _repository.GetByIdAsync(type.Id));
    }

    [Fact]
    public async Task NameExistsAsync_ShouldMatchCaseInsensitivelyAndIgnoreExcludedAndDeletedTypes()
    {
        ObligationType toner = TestDataFactory.CreateObligationType("Toner");
        ObligationType deleted = TestDataFactory.CreateObligationType("Archived");
        await SeedAsync(toner, deleted);
        await _repository.SoftDeleteAsync(deleted.Id);

        Assert.True(await _repository.NameExistsAsync(" toner "));
        Assert.False(await _repository.NameExistsAsync("Toner", toner.Id));
        Assert.False(await _repository.NameExistsAsync("Archived"));
        Assert.False(await _repository.NameExistsAsync("Keyboard"));
    }

    [Fact]
    public async Task RenameAsync_WhenTypeExists_ShouldPersistNewName()
    {
        ObligationType type = TestDataFactory.CreateObligationType("Toner");
        await SeedAsync(type);

        bool renamed = await _repository.RenameAsync(type.Id, "Printer toner");

        Assert.True(renamed);
        Assert.Equal("Printer toner", (await _repository.GetByIdAsync(type.Id))!.Name);
    }

    [Fact]
    public async Task RenameAsync_WhenTypeDoesNotExist_ShouldReturnFalse()
    {
        Assert.False(await _repository.RenameAsync(Guid.NewGuid(), "Toner"));
    }

    [Fact]
    public async Task SoftDeleteAsync_WhenTypeDoesNotExist_ShouldReturnFalse()
    {
        Assert.False(await _repository.SoftDeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task SoftDeleteAsync_WhenTypeIsUsedByObligation_ShouldKeepObligationDescription()
    {
        ObligationType type = TestDataFactory.CreateObligationType("Toner");
        await SeedAsync(type);
        _context.Users.Add(TestDataFactory.CreateUser("user-1", "U1", "User One"));
        OtherObligation obligation = TestDataFactory.CreateObligation("user-1", type);
        _context.OtherObligations.Add(obligation);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        bool deleted = await _repository.SoftDeleteAsync(type.Id);

        Assert.True(deleted);
        OtherObligation stored = await _context.OtherObligations.AsNoTracking().SingleAsync(o => o.Id == obligation.Id);
        Assert.Equal("Toner", stored.Description);
        Assert.Equal(type.Id, stored.ObligationTypeId);
    }

    private async Task SeedAsync(params ObligationType[] types)
    {
        _context.ObligationTypes.AddRange(types);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }
}
