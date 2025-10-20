using Domain;
using Domain.Entities;
using Domain.Repositories.Menus;
using Microsoft.EntityFrameworkCore;

namespace Services.Repositories.Menus;

public class MenuRepository : IMenuRepository
{
    private readonly AppDbContext _dbContext;
        
    public MenuRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public Task<Menu?> GetByIdAsync(Guid Id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<Menu>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Menus
            .Include(m => m.Supplier)
            .Include(x => x.Meals)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Menu menu, CancellationToken cancellationToken = default)
    {
        // This will add the menu and all new meals in the menu
        await _dbContext.Menus.AddAsync(menu, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}