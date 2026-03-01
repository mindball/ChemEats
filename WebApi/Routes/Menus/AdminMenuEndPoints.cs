using System.Diagnostics;
using Domain.Entities;
using Domain.Repositories.MealOrders;
using Domain.Repositories.Menus;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.DTOs.Menus;
using WebApi.Infrastructure.Filters;

namespace WebApi.Routes.Menus;

public static class AdminMenuEndpoints
{
    public static void MapAdminMenuEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(ApiRoutes.AdminMenus.Base)
            .RequireAuthorization()
            .AddEndpointFilter<SupplierSupervisorFilter>();

        group.MapPost("/{menuId:guid}/finalize", FinalizeMenuAsync)
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
    }

    private static async Task<IResult> FinalizeMenuAsync(
        Guid menuId,
        IMenuRepository menuRepository,
        IMealOrderRepository orderRepository,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Admin attempting to finalize menu {MenuId}", menuId);

            Stopwatch stopwatch = Stopwatch.StartNew();

            Menu? menu = await menuRepository.GetForUpdateAsync(menuId, cancellationToken);
            if (menu is null)
            {
                logger.LogWarning("Menu {MenuId} not found", menuId);
                return Results.NotFound($"Menu with ID {menuId} not found.");
            }

            logger.LogInformation(
                "Menu {MenuId} found - Supplier: {SupplierId}, Date: {Date}, ActiveUntil: {ActiveUntil}, IsActive: {IsActive}",
                menu.Id,
                menu.SupplierId,
                menu.Date,
                menu.ActiveUntil,
                menu.IsActive());

            // Domain logic: Finalize menu (set ActiveUntil to now)
            menu.FinalizeMenu();
            await menuRepository.UpdateAsync(menu, cancellationToken);

            logger.LogInformation(
                "Menu {MenuId} finalized - new ActiveUntil: {ActiveUntil}",
                menu.Id,
                menu.ActiveUntil);

            // Mark all pending orders as completed
            (int completedCount, decimal totalAmount) = await orderRepository
                .MarkPendingOrdersAsCompletedForMenuAsync(menuId, cancellationToken);

            stopwatch.Stop();

            logger.LogInformation(
                "Menu {MenuId} finalized successfully: {CompletedCount} orders marked as completed, total amount: {TotalAmount:C} in {ElapsedMs} ms",
                menuId,
                completedCount,
                totalAmount,
                stopwatch.ElapsedMilliseconds);

            FinalizeMenuResponseDto response = new(
                menuId,
                menu.ActiveUntil,
                completedCount,
                totalAmount);

            return Results.Ok(response);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogWarning("Concurrency conflict while finalizing menu {MenuId}", menuId);
            return Results.Conflict("Another user modified this menu. Please refresh and try again.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error finalizing menu {MenuId}: {ErrorMessage}",
                menuId,
                ex.Message);
            throw;
        }
    }
}
