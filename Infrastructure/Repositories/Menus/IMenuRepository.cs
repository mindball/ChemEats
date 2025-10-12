using Domain;
using Domain.Entities;
using Domain.Repositories.Menus;

namespace Infrastructure.Repositories.Menus;

public class MenuRepository : IMenuRepository
{
    private readonly AppDbContext _dbContext;
        
    public MenuRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public Task<Menu?> GetByIdAsync(Menu Id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Menu?> GetByIdAsync(MenuId Id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Menu>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task AddAsync(Menu menu, CancellationToken cancellationToken = default)
    {
        // This will add the menu and all new meals in the menu
        await _dbContext.Menus.AddAsync(menu, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}