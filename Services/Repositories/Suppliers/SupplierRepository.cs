using Domain;
using Domain.Entities;
using Domain.Repositories.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Services.Repositories.Suppliers;

public class SupplierRepository : ISupplierRepository
{
    private readonly AppDbContext _db;

    public SupplierRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Suppliers
            .Include(s => s.Menus)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Supplier?> DeleteAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(supplier, nameof(supplier));
        EntityEntry<Supplier> entry = _db.Suppliers.Remove(supplier);
        await _db.SaveChangesAsync(cancellationToken);
        return entry.Entity;
    }

    public async Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        await _db.Suppliers.AddAsync(supplier, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<Supplier>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        DateTime today = DateTime.Today;
        DateTime startDate = new DateTime(today.Year, today.Month, 1).AddMonths(-2);
        DateTime endDate = startDate.AddMonths(1);

        List<Supplier> suppliers = await _db.Suppliers
            .Include(s => s.Menus.Where(m => m.Date >= startDate))
            .ToListAsync(cancellationToken);

        return suppliers;
    }

    public async Task<Supplier> UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        _db.Suppliers.Update(supplier);
        await _db.SaveChangesAsync(cancellationToken);
        return supplier;
    }
}