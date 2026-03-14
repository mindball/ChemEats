using ChemEats.Tests.TestInfrastructure;
using Domain;
using Domain.Entities;
using Domain.Infrastructure.Identity;
using Services.Repositories.Suppliers;

namespace ChemEats.Tests.Services.Repositories.Suppliers;

public sealed class SupplierRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly SupplierRepository _repository;

    public SupplierRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _repository = new SupplierRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ShouldReturnSupplier()
    {
        Supplier supplier = TestDataFactory.CreateSupplier();

        await _repository.AddAsync(supplier);

        Supplier? result = await _repository.GetByIdAsync(supplier.Id);
        Assert.NotNull(result);
        Assert.Equal(supplier.Name, result.Name);
    }

    [Fact]
    public async Task GetBySupervisorIdAsync_ShouldReturnMatchingSupplier()
    {
        string supervisorId = Guid.NewGuid().ToString();
        ApplicationUser supervisor = TestDataFactory.CreateUser(supervisorId, "SPV", "Supervisor User");

        Supplier supplier = TestDataFactory.CreateSupplier();
        supplier.AssignSupervisor(supervisorId);

        await _context.Users.AddAsync(supervisor);
        await _repository.AddAsync(supplier);

        Supplier? result = await _repository.GetBySupervisorIdAsync(supervisorId);

        Assert.NotNull(result);
        Assert.Equal(supplier.Id, result.Id);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnPersistedSuppliers()
    {
        Supplier first = TestDataFactory.CreateSupplier(name: "First");
        Supplier second = TestDataFactory.CreateSupplier(name: "Second");

        await _repository.AddAsync(first);
        await _repository.AddAsync(second);

        IEnumerable<Supplier> suppliers = await _repository.GetAllAsync();

        Assert.Equal(2, suppliers.Count());
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistChanges()
    {
        Supplier supplier = TestDataFactory.CreateSupplier(name: "Before");
        await _repository.AddAsync(supplier);
        _context.ChangeTracker.Clear();

        Supplier updated = new(supplier.Id, "After", supplier.VatNumber, supplier.PaymentTerms);

        Supplier result = await _repository.UpdateAsync(updated);

        Assert.Equal("After", result.Name);
        Supplier? persisted = await _repository.GetByIdAsync(supplier.Id);
        Assert.NotNull(persisted);
        Assert.Equal("After", persisted.Name);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveSupplier()
    {
        Supplier supplier = TestDataFactory.CreateSupplier();
        await _repository.AddAsync(supplier);

        Supplier? deleted = await _repository.DeleteAsync(supplier);

        Assert.NotNull(deleted);
        Supplier? fromDb = await _repository.GetByIdAsync(supplier.Id);
        Assert.Null(fromDb);
    }

    [Fact]
    public async Task DeleteAsync_WhenSupplierIsNull_ShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.DeleteAsync(null!));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
