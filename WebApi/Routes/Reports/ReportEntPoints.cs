using Domain.Entities;
using Domain.Infrastructure.Identity;
using Domain.Models.Orders;
using Domain.Repositories.MealOrders;
using Domain.Repositories.Menus;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Shared;
using Shared.DTOs.Reports;
using WebApi.Infrastructure.Filters;

namespace WebApi.Routes.Reports;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(ApiRoutes.Reports.Base)
            .RequireAuthorization()
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();

        group.MapGet(ApiRoutes.Reports.MenuReportDataRoute, GetMenuReportDataAsync)
            .RequireAuthorization("SupplierManagementPolicy")
            .AddEndpointFilter<SupplierSupervisorFilter>();
    }

    private static async Task<IResult> GetMenuReportDataAsync(
        Guid menuId,
        IMenuRepository menuRepository,
        IMealOrderRepository orderRepository,
        UserManager<ApplicationUser> userManager,
        IMapper mapper,
        HttpContext httpContext,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            ApplicationUser? user = await userManager.GetUserAsync(httpContext.User);
            if (user is null)
            {
                logger.LogWarning("Report request for menu {MenuId} rejected - unauthorized", menuId);
                return Results.Unauthorized();
            }

            Menu? menu = await menuRepository.GetByIdAsync(menuId, cancellationToken);
            if (menu is null)
            {
                logger.LogWarning("Report request - menu {MenuId} not found by {UserId}", menuId, user.Id);
                return Results.NotFound(new { Message = $"Menu with id '{menuId}' not found." });
            }

            IReadOnlyList<UserOrderItem> orders =
                    await orderRepository.GetAllOrdersByMenuAsync(menuId, cancellationToken);

            logger.LogInformation(
                "User {UserId} requested report data for menu {MenuId} — {OrderCount} total orders",
                user.Id,
                menuId,
                orders.Count);

            MenuReportDto report = new(
                menu.Id,
                menu.Supplier?.Name ?? string.Empty,
                menu.Date,
                menu.ActiveUntil,
                orders.Select(mapper.Map<MenuReportOrderDto>).ToList());

            return Results.Ok(report);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error building report data for menu {MenuId}: {ErrorMessage}",
                menuId,
                ex.Message);
            throw;
        }
    }
}
