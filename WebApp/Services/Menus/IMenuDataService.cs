using Shared.DTOs.Menus;

namespace WebApp.Services.Menus;

public interface IMenuDataService
{
    Task<MenuDto?> AddMenuAsync(CreateMenuDto menu);
    Task<IEnumerable<MenuDto>> GetAllMenusAsync();
}