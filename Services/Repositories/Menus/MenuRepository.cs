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
    public async Task<Menu?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbContext.Menus
            .Include(m => m.Supplier)
            .Include(m => m.Meals)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Menu>> GetAllAsync(bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IQueryable<Menu> query = _dbContext.Menus
            .Include(m => m.Supplier)
            .Include(m => m.Meals)
            .AsNoTracking();

        if (!includeDeleted)
            query = query.Where(m => !m.IsDeleted);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Menu menu, CancellationToken cancellationToken = default)
    {
        await _dbContext.Menus.AddAsync(menu, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UpdateDateAsync(Guid menuId, DateTime newDate, CancellationToken cancellationToken = default)
    {
        Menu? menu = await _dbContext.Menus.FirstOrDefaultAsync(x => x.Id == menuId, cancellationToken);
        if (menu is null) return false;

        menu.UpdateDate(newDate);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SoftDeleteAsync(Guid menuId, CancellationToken cancellationToken = default)
    {
        Menu? menu = await _dbContext.Menus.FirstOrDefaultAsync(x => x.Id == menuId, cancellationToken);
        if (menu is null) return false;

        menu.SoftDelete();

        // Optionally cancel orders for that date too
        await _dbContext.MealOrders
            .Where(o => o.Date.Date == menu.Date.Date && !o.IsDeleted)
            .ForEachAsync(o => o.Cancel(), cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}