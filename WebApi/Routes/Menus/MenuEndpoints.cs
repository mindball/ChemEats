using Domain.Entities;
using Domain.Repositories.Menus;
using MapsterMapper; 
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Menus;
using WebApi.Infrastructure.Filters;

namespace WebApi.Routes.Menus;

public static class MenuEndpoints
{
    public static void MapMenuEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/menus");

        group.MapPost("", async ([FromBody] CreateMenuDto menuDto, IMenuRepository menuRepository, IMapper mapper, CancellationToken cancellationToken) =>
        {
            Menu menu = mapper.Map<Menu>(menuDto);
            await menuRepository.AddAsync(menu, cancellationToken);
            MenuDto createdDto = mapper.Map<MenuDto>(menu);
            return Results.Created($"/api/menus/{menu.Id}", createdDto);
        }).RequireAuthorization().AddEndpointFilter<AuthorizedRequestLoggingFilter>();

        group.MapGet("/{menuId:guid}", async (Guid menuId, IMenuRepository menuRepository, IMapper mapper, CancellationToken cancellationToken) =>
        {
            Menu? menu = await menuRepository.GetByIdAsync(menuId, cancellationToken);
            return menu != null
                ? Results.Ok(mapper.Map<MenuDto>(menu))
                : Results.NotFound();
        }).AllowAnonymous();

        group.MapGet("", async (IMenuRepository menuRepository, CancellationToken cancellationToken, IMapper mapper, [FromQuery] bool includeDeleted = false) =>
        {
            IEnumerable<Menu> menus = await menuRepository.GetAllAsync(includeDeleted, cancellationToken);
            IEnumerable<MenuDto> dto = menus.Select(mapper.Map<MenuDto>);
            return Results.Ok(dto);
        }).AllowAnonymous();

        group.MapPut("/{menuId:guid}/date", async (Guid menuId, [FromBody] DateTime newDate, IMenuRepository repo, CancellationToken ct) =>
        {
            bool ok = await repo.UpdateDateAsync(menuId, newDate, ct);
            return ok ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization();

        // group.MapPost("/{menuId:guid}/deactivate", async (Guid menuId, IMenuRepository repo, CancellationToken ct) =>
        // {
        //     bool ok = await repo.DeactivateAsync(menuId, ct);
        //     return ok ? Results.NoContent() : Results.NotFound();
        // }).RequireAuthorization();
        //
        // group.MapPost("/{menuId:guid}/activate", async (Guid menuId, IMenuRepository repo, CancellationToken ct) =>
        // {
        //     bool ok = await repo.ActivateAsync(menuId, ct);
        //     return ok ? Results.NoContent() : Results.NotFound();
        // }).RequireAuthorization();

        group.MapDelete("/{menuId:guid}", async (Guid menuId, IMenuRepository repo, CancellationToken ct) =>
        {
            bool ok = await repo.SoftDeleteAsync(menuId, ct);
            return ok ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization();
    }
}
