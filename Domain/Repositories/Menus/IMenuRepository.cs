using Domain.Entities;
using System.Threading;

namespace Domain.Repositories.Menus;

public interface IMenuRepository
{
    Task<Menu?> GetByIdAsync(MenuId Id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Menu>> GetAllAsync(CancellationToken cancellationToken = default); 
    Task AddAsync(Menu menu, CancellationToken cancellationToken = default);
    
}