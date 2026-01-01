using Domain.Entities;

namespace Domain.Repositories.Menus;

public interface IMenuRepository
{
    Task<Menu?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Menu>> GetAllAsync(bool includeDeleted = false, CancellationToken cancellationToken = default); 
    Task AddAsync(Menu menu, CancellationToken cancellationToken = default);

    Task<bool> UpdateDateAsync(Guid menuId, DateTime newDate, CancellationToken cancellationToken = default);
    // Task<bool> DeactivateAsync(Guid menuId, CancellationToken cancellationToken = default);
    // Task<bool> ActivateAsync(Guid menuId, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid menuId, CancellationToken cancellationToken = default);
}