using Domain.Entities;
using Domain.Infrastructure.Identity;
using Domain.Repositories.Menus;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Menus;
using WebApi.Infrastructure.Filters;

namespace WebApi.Routes.Menus;

public static class MenuEndpoints
{
    public static void MapMenuEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/menus");

        group.MapPost("",
            async ([FromBody] CreateMenuDto menuDto, IMenuRepository menuRepository, IMapper mapper,
                    ILogger<Program> logger, HttpContext httpContext, UserManager<ApplicationUser> userManager,
                    CancellationToken cancellationToken)
                =>
            {
                try
                {
                    ApplicationUser? user = await userManager.GetUserAsync(httpContext.User);
                    if (user is null)
                    {
                        logger.LogWarning("Unauthorized menu creation attempt");
                        return Results.Unauthorized();
                    }

                    logger.LogInformation(
                        "User {User} creating menu for supplier {SupplierId} on {Date}",
                        httpContext.User.Identity?.Name,
                        menuDto.SupplierId,
                        menuDto.Date);

                    Menu menu = mapper.Map<Menu>(menuDto);
                    await menuRepository.AddAsync(menu, cancellationToken);

                    logger.LogInformation(
                        "Menu {MenuId} created successfully by {User}",
                        menu.Id,
                        httpContext.User.Identity?.Name);

                    MenuDto createdDto = mapper.Map<MenuDto>(menu);
                    return Results.Created($"/api/menus/{menu.Id}", createdDto);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, 
                        "Failed to create menu for supplier {SupplierId} on {Date}: {ErrorMessage}",
                        menuDto.SupplierId,
                        menuDto.Date,
                        ex.Message);
                    throw;
                }
            }).RequireAuthorization().AddEndpointFilter<AuthorizedRequestLoggingFilter>();

        group.MapGet("/{menuId:guid}", async (
            Guid menuId, 
            IMenuRepository menuRepository, 
            IMapper mapper, 
            ILogger<Program> logger,
            CancellationToken cancellationToken) 
            =>
        {
            try
            {
                logger.LogInformation("Retrieving menu by ID: {MenuId}", menuId);
                
                Menu? menu = await menuRepository.GetByIdAsync(menuId, cancellationToken);
                
                if (menu != null)
                {
                    logger.LogInformation("Menu {MenuId} found successfully", menuId);
                    return Results.Ok(mapper.Map<MenuDto>(menu));
                }
                
                logger.LogWarning("Menu {MenuId} not found", menuId);
                return Results.NotFound();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, 
                    "Error retrieving menu {MenuId}: {ErrorMessage}", 
                    menuId, 
                    ex.Message);
                throw;
            }
        }).RequireAuthorization().AddEndpointFilter<AuthorizedRequestLoggingFilter>();

        group.MapGet("", async (
            IMenuRepository menuRepository, 
            CancellationToken cancellationToken, 
            IMapper mapper,
            ILogger<Program> logger,
            [FromQuery] bool includeDeleted = false) =>
        {
            try
            {
                logger.LogInformation("Retrieving all menus (includeDeleted: {IncludeDeleted})", includeDeleted);
                System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                
                IEnumerable<Menu> menus = await menuRepository.GetAllAsync(includeDeleted, cancellationToken);
                IEnumerable<MenuDto> dto = menus.Select(mapper.Map<MenuDto>);
                int count = dto.Count();
                
                sw.Stop();
                logger.LogInformation(
                    "Retrieved {Count} menus in {ElapsedMs} ms (includeDeleted: {IncludeDeleted})",
                    count,
                    sw.ElapsedMilliseconds,
                    includeDeleted);
                
                return Results.Ok(dto);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, 
                    "Error retrieving all menus (includeDeleted: {IncludeDeleted}): {ErrorMessage}",
                    includeDeleted,
                    ex.Message);
                throw;
            }
        }).RequireAuthorization().AddEndpointFilter<AuthorizedRequestLoggingFilter>();

        group.MapPut("/{menuId:guid}/date", async (
            Guid menuId, 
            [FromBody] DateTime newDate, 
            IMenuRepository repo,
            ILogger<Program> logger,
            HttpContext context,
            CancellationToken ct) =>
        {
            try
            {
                logger.LogInformation(
                    "User {User} updating menu {MenuId} date to {NewDate}",
                    context.User.Identity?.Name,
                    menuId,
                    newDate);
                
                bool ok = await repo.UpdateDateAsync(menuId, newDate, ct);
                
                if (ok)
                {
                    logger.LogInformation(
                        "Menu {MenuId} date updated successfully to {NewDate} by {User}",
                        menuId,
                        newDate,
                        context.User.Identity?.Name);
                    return Results.NoContent();
                }
                
                logger.LogWarning(
                    "Menu {MenuId} not found for date update by {User}",
                    menuId,
                    context.User.Identity?.Name);
                return Results.NotFound();
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Error updating menu {MenuId} date to {NewDate} by {User}: {ErrorMessage}",
                    menuId,
                    newDate,
                    context.User.Identity?.Name,
                    ex.Message);
                throw;
            }
        }).RequireAuthorization().AddEndpointFilter<AuthorizedRequestLoggingFilter>();

        group.MapDelete("/{menuId:guid}", async (
            Guid menuId, 
            IMenuRepository repo, 
            ILogger<Program> logger, 
            HttpContext context, 
            CancellationToken ct) =>
        {
            try
            {
                logger.LogWarning(
                    "User {User} attempting to delete menu {MenuId}",
                    context.User.Identity?.Name,
                    menuId);

                bool ok = await repo.SoftDeleteAsync(menuId, ct);
                
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
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Error deleting menu {MenuId} by {User}: {ErrorMessage}",
                    menuId,
                    context.User.Identity?.Name,
                    ex.Message);
                throw;
            }
        }).RequireAuthorization().AddEndpointFilter<AuthorizedRequestLoggingFilter>();
    }
}
