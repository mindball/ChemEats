using Shared.DTOs.Menus;

namespace WebApp.Services.Menus;

public interface IMenuDataService
{
    Task<MenuDto?> AddMenuAsync(CreateMenuDto menu);
    Task<IEnumerable<MenuDto>> GetAllMenusAsync(bool includeDeleted = false);

    Task<bool> UpdateMenuDateAsync(Guid menuId, DateTime newDate);
    // Task<bool> DeactivateMenuAsync(Guid menuId);
    // Task<bool> ActivateMenuAsync(Guid menuId);
    Task<bool> SoftDeleteMenuAsync(Guid menuId);
}