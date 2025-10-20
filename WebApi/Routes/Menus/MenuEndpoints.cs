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
        var group = app.MapGroup("/api/menus");

        group.MapPost("", async (
            [FromBody] CreateMenuDto menuDto,
            IMenuRepository menuRepository,
            IMapper mapper, 
            CancellationToken cancellationToken) =>
        {
            if (menuDto == null)
                return Results.BadRequest("Menu data is required.");

            var menu = mapper.Map<Menu>(menuDto);

            await menuRepository.AddAsync(menu, cancellationToken);

            var createdDto = mapper.Map<MenuDto>(menu);
            return Results.Created($"/api/menus/{menu.Id}", createdDto);
        }).RequireAuthorization().AddEndpointFilter<AuthorizedRequestLoggingFilter>(); 

        // Get menus by supplier
        group.MapGet("/supplier/{supplierId:guid}", async (
            Guid supplierId,
            IMenuRepository menuRepository,
            IMapper mapper,
            CancellationToken cancellationToken) =>
        {
            // var menus = await menuRepository.GetBySupplierIdAsync(new SupplierId(supplierId), cancellationToken);
            // var dto = mapper.Map<IEnumerable<MenuDto>>(menus);
            // return Results.Ok(dto);
            return Results.Ok();
        }).AllowAnonymous(); ;

        group.MapGet("/{menuId:guid}", async (
            Guid menuId,
            IMenuRepository menuRepository,
            IMapper mapper,
            CancellationToken cancellationToken) =>
        {
            Menu? menu = await menuRepository.GetByIdAsync(menuId, cancellationToken);
            return menu != null
                ? Results.Ok(mapper.Map<MenuDto>(menu))
                : Results.NotFound();
        }).AllowAnonymous(); ;

        // ✅ Delete a menu
        group.MapDelete("/{menuId:guid}", async (
            Guid menuId,
            IMenuRepository menuRepository,
            CancellationToken cancellationToken) =>
        {
            Menu? menu = await menuRepository.GetByIdAsync(menuId, cancellationToken);
            if (menu == null)
                return Results.NotFound();

            // await menuRepository.DeleteAsync(menu, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization();

        group.MapGet("", async (
            IMenuRepository menuRepository,
            IMapper mapper,
            CancellationToken cancellationToken) =>
        {
            // You may need to implement GetAllAsync in your repository
            var menus = await menuRepository.GetAllAsync(cancellationToken);
            var dto = menus.Select(menu => mapper.Map<MenuDto>(menu));
            return Results.Ok(dto);
        }).AllowAnonymous();
    }
}
