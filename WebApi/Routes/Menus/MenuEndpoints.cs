    using Domain.Entities;
using Domain.Infrastructure.Exceptions;
using Domain.Repositories.MealOrders;
using Domain.Repositories.Menus;
using Domain.Repositories.Suppliers;
using MapsterMapper;
using MenuParser.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Shared.DTOs.Meals;
using Shared.DTOs.Menus;
using WebApi.Infrastructure.Filters;

namespace WebApi.Routes.Menus;

public static class MenuEndpoints
{
    public static void MapMenuEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(ApiRoutes.Menus.Base)
            .WithTags("Menus")
            .RequireAuthorization().AddEndpointFilter<AuthorizedRequestLoggingFilter>();

        group.MapPost("", CreateMenuAsync).AddEndpointFilter<SupplierSupervisorFilter>();
        group.MapPost(ApiRoutes.Menus.ParseFile, ParseMenuFileAsync)
            .AddEndpointFilter<SupplierSupervisorFilter>()
            .DisableAntiforgery();
        group.MapGet("", GetAllMenusAsync);
        group.MapGet(ApiRoutes.Menus.Active, GetActiveMenusAsync);
        group.MapGet(ApiRoutes.Menus.ByIdRoute, GetMenuByIdAsync);
        group.MapGet(ApiRoutes.Menus.BySupplierRoute, GetMenusBySupplierAsync);
        group.MapPut(ApiRoutes.Menus.UpdateDateRoute, UpdateMenuDateAsync).AddEndpointFilter<SupplierSupervisorFilter>();
        group.MapPut(ApiRoutes.Menus.UpdateActiveUntilRoute, UpdateMenuActiveUntilAsync).AddEndpointFilter<SupplierSupervisorFilter>();
        group.MapDelete(ApiRoutes.Menus.ByIdRoute, SoftDeleteMenuAsync).AddEndpointFilter<SupplierSupervisorFilter>();
    }

    private static async Task<IResult> GetMenuByIdAsync(
        Guid menuId,
        IMenuRepository menuRepository,
        IMealOrderRepository orderRepository,
        IMapper mapper,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving menu by ID: {MenuId}", menuId);

        Menu? menu = await menuRepository.GetByIdAsync(menuId, cancellationToken);
        if (menu is null)
        {
            logger.LogWarning("Menu {MenuId} not found", menuId);
            return Results.NotFound(new { Message = $"Menu with id '{menuId}' not found" });
        }

        MenuDto dto = mapper.Map<MenuDto>(menu);

        Dictionary<Guid, int> pendingCounts = await orderRepository
            .GetPendingOrdersCountByMenuIdsAsync([menuId], cancellationToken);

        MenuDto enrichedDto = dto with { PendingOrdersCount = pendingCounts.GetValueOrDefault(menuId) };
        return Results.Ok(enrichedDto);
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
            return Results.BadRequest(new { ex.Message });
        }
    }

    private static async Task<IResult> ParseMenuFileAsync(
        IFormFile? file,
        IMenuFileParser parser,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { Message = "No file provided." });

        if (!parser.IsSupported(file.FileName))
            return Results.BadRequest(new { Message = "Unsupported file format. Supported: .csv, .xlsx, .docx" });

        try
        {
            logger.LogInformation("Parsing menu file: {FileName} ({Size} bytes)", file.FileName, file.Length);

            await using Stream stream = file.OpenReadStream();
            IReadOnlyList<MenuParser.Models.ParsedMeal> meals = await parser.ParseAsync(stream, file.FileName, cancellationToken);

            List<CreateMealDto> result = meals
                .Select(m => new CreateMealDto(m.Name, m.Price))
                .ToList();

            logger.LogInformation("Parsed {Count} meals from {FileName}", result.Count, file.FileName);
            return Results.Ok(result);
        }
        catch (NotSupportedException ex)
        {
            logger.LogWarning(ex, "Unsupported file format: {FileName}", file.FileName);
            return Results.BadRequest(new { ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "AI parsing error for file: {FileName}", file.FileName);
            return Results.BadRequest(new { ex.Message });
        }
    }

    private static async Task<IResult> GetAllMenusAsync(
        IMenuRepository menuRepository,
        IMealOrderRepository orderRepository,
        IMapper mapper,
        CancellationToken cancellationToken,
        [FromQuery] bool includeDeleted = false)
    {
        IReadOnlyList<Menu> menus = await menuRepository.GetAllAsync(includeDeleted, cancellationToken);
        List<MenuDto> dtos = mapper.Map<List<MenuDto>>(menus);

        Dictionary<Guid, int> pendingCounts = await orderRepository
            .GetPendingOrdersCountByMenuIdsAsync(dtos.Select(d => d.Id), cancellationToken);

        List<MenuDto> enrichedDtos = dtos
            .Select(d => d with { PendingOrdersCount = pendingCounts.GetValueOrDefault(d.Id) })
            .ToList();

        return Results.Ok(enrichedDtos);
    }

    private static async Task<IResult> GetActiveMenusAsync(
        IMenuRepository menuRepository,
        IMealOrderRepository orderRepository,
        IMapper mapper,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Menu> menus = await menuRepository.GetActiveMenusAsync(cancellationToken);
        List<MenuDto> dtos = mapper.Map<List<MenuDto>>(menus);

        Dictionary<Guid, int> pendingCounts = await orderRepository
            .GetPendingOrdersCountByMenuIdsAsync(dtos.Select(d => d.Id), cancellationToken);

        List<MenuDto> enrichedDtos = dtos
            .Select(d => d with { PendingOrdersCount = pendingCounts.GetValueOrDefault(d.Id) })
            .ToList();

        return Results.Ok(enrichedDtos);
    }

    private static async Task<IResult> GetMenusBySupplierAsync(
        Guid supplierId,
        IMenuRepository menuRepository,
        IMealOrderRepository orderRepository,
        IMapper mapper,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Menu> menus = await menuRepository.GetBySupplierAsync(supplierId, cancellationToken);
        List<MenuDto> dtos = mapper.Map<List<MenuDto>>(menus);

        Dictionary<Guid, int> pendingCounts = await orderRepository
            .GetPendingOrdersCountByMenuIdsAsync(dtos.Select(d => d.Id), cancellationToken);

        List<MenuDto> enrichedDtos = dtos
            .Select(d => d with { PendingOrdersCount = pendingCounts.GetValueOrDefault(d.Id) })
            .ToList();

        return Results.Ok(enrichedDtos);
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

        try
        {
            logger.LogWarning(
                "User {User} attempting to update date menu {MenuId}",
                context.User.Identity?.Name,
                menuId);

            await menuRepository.UpdateDateAsync(menu, newDate, cancellationToken);

            logger.LogInformation("Menu {MenuId} date updated to {NewDate} by {User}", menuId, newDate,
                context.User.Identity?.Name);
            return Results.NoContent();
        }
        catch (DomainException ex)
        {
            logger.LogWarning(ex, "Failed to update menu {MenuId} date by {User}: {ErrorMessage}", menuId,
                context.User.Identity?.Name, ex.Message);
            return Results.BadRequest(new { ex.Message });
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

        if (menu.IsDeleted)
        {
            logger.LogWarning("Cannot update deleted menu {MenuId}", menuId);
            return Results.BadRequest(new { Message = "Cannot update ActiveUntil of deleted menu" });
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
            return Results.BadRequest(new { ex.Message });
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
            return Results.BadRequest(new { ex.Message });
        }
        catch (DomainException ex)
        {
            logger.LogWarning(ex,
                "Domain validation failed for menu {MenuId} by {User}: {ErrorMessage}",
                menuId,
                context.User.Identity?.Name,
                ex.Message);
            return Results.BadRequest(new { ex.Message });
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