using Domain.Entities;
using Domain.Infrastructure.Exceptions;
using Domain.Repositories.Menus;
using Domain.Repositories.Suppliers;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Menus;
using WebApi.Infrastructure.Filters;

namespace WebApi.Routes.Menus;

public static class MenuEndpoints
{
    public static void MapMenuEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("api/menus")
            .WithTags("Menus")
            .RequireAuthorization().AddEndpointFilter<AuthorizedRequestLoggingFilter>();

        group.MapPost("", CreateMenuAsync);
        group.MapGet("", GetAllMenusAsync);
        group.MapGet("active", GetActiveMenusAsync);
        group.MapGet("supplier/{supplierId:guid}", GetMenusBySupplierAsync);
        group.MapPut("{menuId:guid}/date", UpdateMenuDateAsync);
        group.MapPut("{menuId:guid}/active-until", UpdateMenuActiveUntilAsync);
        group.MapDelete("{menuId:guid}", SoftDeleteMenuAsync);
    }

    private static async Task<IResult> CreateMenuAsync(
        [FromBody] CreateMenuDto dto,
        IMenuRepository menuRepository,
        ISupplierRepository supplierRepository,
        IMapper mapper,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating menu for supplier {SupplierId} on {Date} active until {ActiveUntil}",
            dto.SupplierId, dto.Date, dto.ActiveUntil);

        Supplier? supplier = await supplierRepository.GetByIdAsync(dto.SupplierId, cancellationToken);
        if (supplier is null)
        {
            logger.LogWarning("Supplier {SupplierId} not found", dto.SupplierId);
            return Results.NotFound(new { Message = "Supplier not found" });
        }

        bool exists = await menuRepository.ExistsAsync(dto.SupplierId, dto.Date, cancellationToken);
        if (exists)
        {
            logger.LogWarning("Menu already exists for supplier {SupplierId} on {Date}", dto.SupplierId, dto.Date);
            return Results.Conflict(new { Message = "Menu already exists for this supplier and date" });
        }

        try
        {
            List<Meal> meals = dto.Meals
                .Select(m => Meal.Create(Guid.Empty, m.Name, m.Price))
                .ToList();

            Menu menu = Menu.Create(dto.SupplierId, dto.Date, dto.ActiveUntil, meals);

            await menuRepository.AddAsync(menu, cancellationToken);

            MenuDto result = mapper.Map<MenuDto>(menu);

            logger.LogInformation("Menu {MenuId} created successfully", menu.Id);
            return Results.Created($"/api/menus/{menu.Id}", result);
        }
        catch (DomainException ex)
        {
            logger.LogWarning(ex, "Domain validation failed while creating menu");
            return Results.BadRequest(new { Message = ex.Message });
        }
    }

    private static async Task<IResult> GetAllMenusAsync(
        IMenuRepository menuRepository,
        IMapper mapper,
        CancellationToken cancellationToken,
        [FromQuery] bool includeDeleted = false)
    {
        IReadOnlyList<Menu> menus = await menuRepository.GetAllAsync(includeDeleted, cancellationToken);
        List<MenuDto> dtos = mapper.Map<List<MenuDto>>(menus);
        return Results.Ok(dtos);
    }

    private static async Task<IResult> GetActiveMenusAsync(
        IMenuRepository menuRepository,
        IMapper mapper,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Menu> menus = await menuRepository.GetActiveMenusAsync(cancellationToken);
        List<MenuDto> dtos = mapper.Map<List<MenuDto>>(menus);
        return Results.Ok(dtos);
    }

    private static async Task<IResult> GetMenusBySupplierAsync(
        Guid supplierId,
        IMenuRepository menuRepository,
        IMapper mapper,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Menu> menus = await menuRepository.GetBySupplierAsync(supplierId, cancellationToken);
        List<MenuDto> dtos = mapper.Map<List<MenuDto>>(menus);
        return Results.Ok(dtos);
    }

    private static async Task<IResult> UpdateMenuDateAsync(
        Guid menuId,
        [FromBody] DateTime newDate,
        IMenuRepository menuRepository,
        ILogger<Program> logger,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        Menu? menu = await menuRepository.GetByIdAsync(menuId, cancellationToken);
        if (menu is null)
        {
            logger.LogWarning("Menu {MenuId} not found", menuId);
            return Results.NotFound();
        }

        if (!menu.IsActive())
        {
            logger.LogWarning("Cannot update inactive menu {MenuId}", menuId);
            return Results.BadRequest(new { Message = "Cannot update date of inactive menu" });
        }

        try
        {
            logger.LogWarning(
                "User {User} attempting to update date menu {MenuId}",
                context.User.Identity?.Name,
                menuId);

            menu.UpdateDate(newDate);
            await menuRepository.UpdateAsync(menu, cancellationToken);

            logger.LogInformation("Menu {MenuId} date updated to {NewDate} by {User}", menuId, newDate,
                context.User.Identity?.Name);
            return Results.NoContent();
        }
        catch (DomainException ex)
        {
            logger.LogWarning(ex, "Failed to update menu {MenuId} date by {User}: {ErrorMessage}", menuId,
                context.User.Identity?.Name, ex.Message);
            return Results.BadRequest(new { Message = ex.Message });
        }
    }

    private static async Task<IResult> UpdateMenuActiveUntilAsync(
        Guid menuId,
        [FromBody] DateTime newActiveUntil,
        IMenuRepository menuRepository,
        ILogger<Program> logger,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        Menu? menu = await menuRepository.GetByIdAsync(menuId, cancellationToken);
        if (menu is null)
        {
            logger.LogWarning("Menu {MenuId} not found", menuId);
            return Results.NotFound();
        }

        if (!menu.IsActive())
        {
            logger.LogWarning("Cannot update inactive menu {MenuId}", menuId);
            return Results.BadRequest(new { Message = "Cannot update ActiveUntil of inactive menu" });
        }

        try
        {
            logger.LogWarning(
                "User {User} attempting to update active untile date menu {MenuId}",
                context.User.Identity?.Name,
                menuId);

            menu.UpdateActiveUntil(newActiveUntil);
            await menuRepository.UpdateAsync(menu, cancellationToken);

            logger.LogInformation("Menu {MenuId} ActiveUntil updated to {NewActiveUntil} by {User}", menuId,
                newActiveUntil, context.User.Identity?.Name);

            return Results.NoContent();
        }
        catch (DomainException ex)
        {
            logger.LogWarning(ex, "Failed to update menu {MenuId} ActiveUntil by {User}: {ErrorMessage}", menuId,
                context.User.Identity?.Name, ex.Message);
            return Results.BadRequest(new { Message = ex.Message });
        }
    }

    private static async Task<IResult> SoftDeleteMenuAsync(
        Guid menuId,
        IMenuRepository repo,
        ILogger<Program> logger,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogWarning(
                "User {User} attempting to delete menu {MenuId}",
                context.User.Identity?.Name,
                menuId);

            bool ok = await repo.SoftDeleteAsync(menuId, cancellationToken);

            if (ok)
            {
                logger.LogInformation(
                    "Menu {MenuId} deleted successfully by {User}",
                    menuId,
                    context.User.Identity?.Name);
                return Results.NoContent();
            }

            logger.LogWarning(
                "Menu {MenuId} not found for deletion by {User}",
                menuId,
                context.User.Identity?.Name);
            return Results.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex,
                "Cannot delete menu {MenuId} by {User}: {ErrorMessage}",
                menuId,
                context.User.Identity?.Name,
                ex.Message);
            return Results.BadRequest(new { Message = ex.Message });
        }
        catch (DomainException ex)
        {
            logger.LogWarning(ex,
                "Domain validation failed for menu {MenuId} by {User}: {ErrorMessage}",
                menuId,
                context.User.Identity?.Name,
                ex.Message);
            return Results.BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error deleting menu {MenuId} by {User}: {ErrorMessage}",
                menuId,
                context.User.Identity?.Name,
                ex.Message);
            throw;
        }
    }
}