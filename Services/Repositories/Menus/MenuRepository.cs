using Domain;
using Domain.Entities;
using Domain.Repositories.Menus;
using Microsoft.EntityFrameworkCore;

namespace Services.Repositories.Menus;

public class MenuRepository : IMenuRepository
{
    private readonly AppDbContext _context;

    public MenuRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Menu?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Menus
            .Include(m => m.Supplier)
            .Include(m => m.Meals)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Menu>> GetAllAsync(bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IQueryable<Menu> query = _context.Menus
            .Include(m => m.Supplier)
            .Include(m => m.Meals)
            .AsNoTracking();

        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<Menu?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Menus
            .Include(m => m.Supplier)
            .Include(m => m.Meals)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }


    public async Task<IReadOnlyList<Menu>> GetBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default)
    {
        return await _context.Menus
            .Include(m => m.Supplier)
            .Include(m => m.Meals)
            .Where(m => m.SupplierId == supplierId && !m.IsDeleted)
            .OrderByDescending(m => m.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Menu>> GetActiveMenusAsync(CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.Now;

        return await _context.Menus
            .Include(m => m.Supplier)
            .Include(m => m.Meals)
            .Where(m => !m.IsDeleted && 
                       m.ActiveUntil >= now)
            .OrderBy(m => m.ActiveUntil)
            .ToListAsync(cancellationToken);
    }

    public async Task<Menu?> GetBySupplierAndDateAsync(Guid supplierId, DateTime date, CancellationToken cancellationToken = default)
    {
        return await _context.Menus
            .Include(m => m.Supplier)
            .Include(m => m.Meals)
            .FirstOrDefaultAsync(
                m => m.SupplierId == supplierId && 
                     m.Date.Date == date.Date && 
                     !m.IsDeleted,
                cancellationToken);
    }

    public async Task AddAsync(Menu menu, CancellationToken cancellationToken = default)
    {
        await _context.Menus.AddAsync(menu, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Menu menu, CancellationToken cancellationToken = default)
    {
        Menu? existingMenu = await _context.Menus
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == menu.Id, cancellationToken);

        if (existingMenu is null)
            throw new InvalidOperationException($"Menu with ID {menu.Id} not found.");

        if (existingMenu.IsDeleted)
            throw new InvalidOperationException("Cannot update deleted menu.");

        _context.Menus.Update(menu);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateDateAsync(Menu menu, DateTime newDate, CancellationToken cancellationToken = default)
    {
        List<MealOrder> orders = await _context.MealOrders
            .Include(o => o.Meal)
            .Where(o => o.Meal.MenuId == menu.Id
                        && !o.IsDeleted
                        && o.Status == MealOrderStatus.Pending)
            .ToListAsync(cancellationToken);

        menu.UpdateDateWithPendingOrders(newDate, orders);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid supplierId, DateTime date, CancellationToken cancellationToken = default)
    {
        return await _context.Menus
            .AnyAsync(
                m => m.SupplierId == supplierId && 
                     m.Date.Date == date.Date && 
                     !m.IsDeleted,
                cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(Guid menuId, CancellationToken cancellationToken = default)
    {
        Menu? menu = await _context.Menus
            .FirstOrDefaultAsync(x => x.Id == menuId, cancellationToken);

        if (menu is null)
            return false;

        // Eager load Meal to avoid null navigation
        List<MealOrder> orders = await _context.MealOrders
            .Include(o => o.Meal)
            .Where(o => o.Meal.MenuId == menuId)
            .ToListAsync(cancellationToken);

        // Use new method that handles pending orders
        menu.SoftDeleteWithPendingOrders(orders);

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}