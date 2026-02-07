using Shared.DTOs.Menus;

namespace WebApp.Services.Menus;

public interface IMenuDataService
{
    Task<MenuDto?> AddMenuAsync(CreateMenuDto menu);
    Task<IEnumerable<MenuDto>> GetAllMenusAsync(bool includeDeleted = false);
    Task<IEnumerable<MenuDto>> GetActiveMenusAsync();
    Task<IEnumerable<MenuDto>> GetMenusBySupplierAsync(Guid supplierId);
    Task<bool> UpdateMenuDateAsync(Guid menuId, DateTime newDate);
    Task<bool> UpdateMenuActiveUntilAsync(Guid menuId, DateTime newActiveUntil);
    Task<bool> SoftDeleteMenuAsync(Guid menuId);
}