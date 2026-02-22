using Domain.Entities;
using Domain.Infrastructure.Identity;
using Domain.Models.Orders;
using Domain.Repositories.MealOrders;
using Domain.Repositories.Menus;
using Microsoft.AspNetCore.Identity;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Shared.DTOs.Menus;
using WebApi.Infrastructure.Filters;
using WebApi.Infrastructure.Reports;

namespace WebApi.Routes.Reports;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("api/reports")
            .RequireAuthorization()
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();

        group.MapGet("/menu/{menuId:guid}", async (
            Guid menuId,
            IMenuRepository menuRepository,
            IMealOrderRepository orderRepository,
            UserManager<ApplicationUser> userManager,
            HttpContext httpContext,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
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
                    logger.LogWarning("Report request - menu {MenuId} not found", menuId);
                    return Results.NotFound(new { Message = $"Menu with id '{menuId}' not found." });
                }

                IReadOnlyList<UserOrderItem> orders =
                    await orderRepository.GetOrdersByMenuAsync(user.Id, menuId, cancellationToken);

                logger.LogInformation(
                    "Generating PDF report for user {UserId} on menu {MenuId} with {OrderCount} orders",
                    user.Id,
                    menuId,
                    orders.Count);

                byte[] pdfBytes = MenuReportDocument.Generate(menu, orders);

                string fileName = $"menu-report-{menuId:N}-{DateTime.Now:yyyyMMdd}.pdf";

                return Results.File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Error generating report for menu {MenuId}: {ErrorMessage}",
                    menuId,
                    ex.Message);
                throw;
            }
        });
    }
}