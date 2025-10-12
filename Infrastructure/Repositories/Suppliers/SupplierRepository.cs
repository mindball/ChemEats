using Domain;
using Domain.Entities;
using Domain.Repositories.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.Repositories.Suppliers;

public class SupplierRepository : ISupplierRepository
{
    private readonly AppDbContext _db;

    public SupplierRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Supplier?> GetByIdAsync(SupplierId id, CancellationToken cancellationToken = default)
    {
        return await _db.Suppliers
            .Include(s => s.Menus)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Supplier?> DeleteAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(supplier, nameof(supplier));
        var entry = _db.Suppliers.Remove(supplier);
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
        return await _db.Suppliers.Include(s => s.Menus).ToListAsync(cancellationToken);
    }

    public Task<Supplier> UpdateAsync(Supplier id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}