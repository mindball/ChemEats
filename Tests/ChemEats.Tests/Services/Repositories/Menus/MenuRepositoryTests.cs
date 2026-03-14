using ChemEats.Tests.TestInfrastructure;
using Domain;
using Domain.Entities;
using Domain.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Services.Repositories.Menus;

namespace ChemEats.Tests.Services.Repositories.Menus;

public sealed class MenuRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly MenuRepository _repository;

    public MenuRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _repository = new MenuRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ShouldReturnMenu()
    {
        Supplier supplier = TestDataFactory.CreateSupplier();
        Menu menu = TestDataFactory.CreateMenu(supplier.Id);

        await _context.Suppliers.AddAsync(supplier);
        await _repository.AddAsync(menu);

        Menu? result = await _repository.GetByIdAsync(menu.Id);

        Assert.NotNull(result);
        Assert.Equal(menu.Id, result.Id);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenSupplierHasMenuOnDate()
    {
        Supplier supplier = TestDataFactory.CreateSupplier();
        Menu menu = TestDataFactory.CreateMenu(supplier.Id);

        await _context.Suppliers.AddAsync(supplier);
        await _repository.AddAsync(menu);

        bool exists = await _repository.ExistsAsync(supplier.Id, menu.Date);

        Assert.True(exists);
    }

    [Fact]
    public async Task GetBySupplierAndDateAsync_ShouldReturnMenu_WhenMatchingMenuExists()
    {
        Supplier supplier = TestDataFactory.CreateSupplier();
        Menu menu = TestDataFactory.CreateMenu(supplier.Id);

        await _context.Suppliers.AddAsync(supplier);
        await _repository.AddAsync(menu);

        Menu? result = await _repository.GetBySupplierAndDateAsync(supplier.Id, menu.Date);

        Assert.NotNull(result);
        Assert.Equal(menu.Id, result.Id);
    }

    [Fact]
    public async Task GetBySupplierAsync_ShouldReturnOnlySupplierMenus()
    {
        Supplier supplier = TestDataFactory.CreateSupplier();
        Supplier secondSupplier = TestDataFactory.CreateSupplier();
        Menu first = TestDataFactory.CreateMenu(supplier.Id);
        Menu second = TestDataFactory.CreateMenu(secondSupplier.Id);

        await _context.Suppliers.AddRangeAsync(supplier, secondSupplier);
        await _repository.AddAsync(first);
        await _repository.AddAsync(second);

        IReadOnlyList<Menu> result = await _repository.GetBySupplierAsync(supplier.Id);

        Assert.Single(result);
        Assert.Equal(first.Id, result[0].Id);
    }

    [Fact]
    public async Task GetActiveMenusAsync_ShouldReturnOnlyActiveMenus()
    {
        Supplier supplier = TestDataFactory.CreateSupplier();
        Menu activeMenu = TestDataFactory.CreateMenu(supplier.Id);
        Menu inactiveMenu = TestDataFactory.CreateMenu(supplier.Id);
        inactiveMenu.FinalizeMenu();

        await _context.Suppliers.AddAsync(supplier);
        await _context.Menus.AddRangeAsync(activeMenu, inactiveMenu);
        await _context.SaveChangesAsync();

        IReadOnlyList<Menu> result = await _repository.GetActiveMenusAsync();

        Assert.Single(result);
        Assert.Equal(activeMenu.Id, result[0].Id);
    }

    [Fact]
    public async Task UpdateAsync_WhenMenuDoesNotExist_ShouldThrow()
    {
        Menu menu = TestDataFactory.CreateMenu(Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.UpdateAsync(menu));
    }

    [Fact]
    public async Task UpdateDateAsync_ShouldUpdateMenuDateAndPendingOrders()
    {
        Supplier supplier = TestDataFactory.CreateSupplier();
        Menu menu = TestDataFactory.CreateMenu(supplier.Id);
        Meal meal = menu.Meals.Single();
        ApplicationUser user = TestDataFactory.CreateUser(Guid.NewGuid().ToString(), "USR", "User Name");
        MealOrder order = TestDataFactory.CreateOrder(user.Id, meal.Id, menu.Date, meal.Price.Amount);

        await _context.Suppliers.AddAsync(supplier);
        await _context.Users.AddAsync(user);
        await _context.Menus.AddAsync(menu);
        await _context.MealOrders.AddAsync(order);
        await _context.SaveChangesAsync();

        DateTime newDate = DateTime.Today.AddDays(5);
        await _repository.UpdateDateAsync(menu, newDate);

        MealOrder persistedOrder = await _context.MealOrders.IgnoreQueryFilters().FirstAsync(x => x.Id == order.Id);
        Assert.Equal(newDate, menu.Date);
        Assert.Equal(newDate, persistedOrder.MenuDate);
    }

    [Fact]
    public async Task SoftDeleteAsync_WhenMenuExists_ShouldDeleteMenuAndPendingOrders()
    {
        Supplier supplier = TestDataFactory.CreateSupplier();
        Menu menu = TestDataFactory.CreateMenu(supplier.Id);
        Meal meal = menu.Meals.Single();
        ApplicationUser user = TestDataFactory.CreateUser(Guid.NewGuid().ToString(), "EMP", "Employee One");
        MealOrder pendingOrder = TestDataFactory.CreateOrder(user.Id, meal.Id, menu.Date, meal.Price.Amount);

        await _context.Suppliers.AddAsync(supplier);
        await _context.Users.AddAsync(user);
        await _context.Menus.AddAsync(menu);
        await _context.MealOrders.AddAsync(pendingOrder);
        await _context.SaveChangesAsync();

        bool deleted = await _repository.SoftDeleteAsync(menu.Id);

        Assert.True(deleted);

        Menu? deletedMenu = await _context.Menus.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == menu.Id);
        MealOrder? deletedOrder = await _context.MealOrders.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == pendingOrder.Id);
        Assert.NotNull(deletedMenu);
        Assert.True(deletedMenu.IsDeleted);
        Assert.NotNull(deletedOrder);
        Assert.True(deletedOrder.IsDeleted);
    }

    [Fact]
    public async Task SoftDeleteAsync_WhenMenuDoesNotExist_ShouldReturnFalse()
    {
        bool result = await _repository.SoftDeleteAsync(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task GetAllAsync_WhenIncludeDeletedIsTrue_ShouldReturnDeletedMenusToo()
    {
        Supplier supplier = TestDataFactory.CreateSupplier();
        Menu deletedMenu = TestDataFactory.CreateMenu(supplier.Id);
        Menu activeMenu = TestDataFactory.CreateMenu(supplier.Id);

        await _context.Suppliers.AddAsync(supplier);
        await _context.Menus.AddRangeAsync(deletedMenu, activeMenu);
        await _context.SaveChangesAsync();

        await _repository.SoftDeleteAsync(deletedMenu.Id);

        IReadOnlyList<Menu> nonDeleted = await _repository.GetAllAsync();
        IReadOnlyList<Menu> allMenus = await _repository.GetAllAsync(includeDeleted: true);

        Assert.Single(nonDeleted);
        Assert.Equal(2, allMenus.Count);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
