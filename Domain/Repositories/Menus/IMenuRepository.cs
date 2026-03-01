using Domain.Entities;

namespace Domain.Repositories.Menus;

public interface IMenuRepository
{
    Task<Menu?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Menu>> GetAllAsync(bool includeDeleted = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Menu>> GetBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Menu>> GetActiveMenusAsync(CancellationToken cancellationToken = default);
    Task<Menu?> GetBySupplierAndDateAsync(Guid supplierId, DateTime date, CancellationToken cancellationToken = default);
    Task AddAsync(Menu menu, CancellationToken cancellationToken = default);
    Task UpdateAsync(Menu menu, CancellationToken cancellationToken = default);
    Task UpdateDateAsync(Menu menu, DateTime newDate, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid supplierId, DateTime date, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid menuId, CancellationToken cancellationToken = default);
    Task<Menu?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
}