using ChemEats.Tests.TestInfrastructure;
using Domain;
using Domain.Entities;
using Services.Repositories.Meals;

namespace ChemEats.Tests.Services.Repositories.Meals;

public sealed class MealRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly MealRepository _repository;

    public MealRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _repository = new MealRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistMeal()
    {
        Supplier supplier = TestDataFactory.CreateSupplier();
        Menu menu = TestDataFactory.CreateMenu(supplier.Id);
        Meal meal = Meal.Create(menu.Id, "Added meal", new Price(11m));

        await _context.Suppliers.AddAsync(supplier);
        await _context.Menus.AddAsync(menu);
        await _context.SaveChangesAsync();

        await _repository.AddAsync(meal);

        Meal? persisted = await _context.Meals.FindAsync(meal.Id);
        Assert.NotNull(persisted);
        Assert.Equal(meal.Name, persisted.Name);
    }

    [Fact]
    public async Task AddAsync_WhenMealIsNull_ShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.AddAsync(null!));
    }

    [Fact]
    public async Task GetByIdAsync_WhenMealExists_ShouldReturnMeal()
    {
        Supplier supplier = TestDataFactory.CreateSupplier();
        Menu menu = TestDataFactory.CreateMenu(supplier.Id);

        await _context.Suppliers.AddAsync(supplier);
        await _context.Menus.AddAsync(menu);
        await _context.SaveChangesAsync();

        Meal meal = menu.Meals.Single();
        Meal? result = await _repository.GetByIdAsync(meal.Id);

        Assert.NotNull(result);
        Assert.Equal(meal.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCancellationIsRequested_ShouldThrow()
    {
        using CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _repository.GetByIdAsync(Guid.NewGuid(), cancellationTokenSource.Token));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
