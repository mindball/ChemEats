using Domain.Infrastructure.Identity;
using Domain.Models.Orders;
using Domain.Repositories.MealOrders;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Shared;
using Shared.DTOs.Orders;
using System.Diagnostics;
using Domain.Repositories.Settings;
using WebApi.Infrastructure.Filters;

namespace WebApi.Routes.Orders;

public static class AdminOrdersEndpoints
{
    public static void MapAdminMealOrderEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(ApiRoutes.AdminOrders.Base)
            .RequireAuthorization("AdminPolicy");

        group.MapGet("/unpaid/{userId}", GetUnpaidOrdersAsync)
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
        group.MapPost("/order-pay", OrderPayAsync)
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
        group.MapGet("/period/{userId}", GetOrdersForPeriodAsync)
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
    }

    private static async Task<IResult> GetUnpaidOrdersAsync(
        string userId,
        Guid? supplierId,
        IMealOrderRepository orderRepository,
        IMapper mapper,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                "Admin retrieving unpaid orders for user {UserId}, SupplierId: {SupplierId}",
                userId,
                supplierId?.ToString() ?? "All");

            Stopwatch stopwatch = Stopwatch.StartNew();

            IReadOnlyList<UserOrderPaymentItem> items =
                await orderRepository.GetUnpaidOrdersAsync(userId, supplierId, cancellationToken);

            List<UserOrderPaymentItemDto> dtos = items.Select(mapper.Map<UserOrderPaymentItemDto>).ToList();

            stopwatch.Stop();
            logger.LogInformation(
                "Admin retrieved {UnpaidCount} unpaid orders for user {UserId} in {ElapsedMs} ms",
                dtos.Count,
                userId,
                stopwatch.ElapsedMilliseconds);

            return Results.Ok(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error retrieving unpaid orders for user {UserId}: {ErrorMessage}",
                userId,
                ex.Message);
            throw;
        }
    }

    private static async Task<IResult> OrderPayAsync(
        OrderPayRequestDto requestDto,
        IMealOrderRepository orderRepository,
        ISettingsRepository settingsRepository,
        UserManager<ApplicationUser> userManager,
        HttpContext httpContext,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(requestDto);

            if (requestDto.OrderIds.Count == 0)
            {
                logger.LogWarning("Order pay rejected - empty order IDs list");
                return Results.BadRequest("At least one order ID is required.");
            }

            ApplicationUser? admin = await userManager.GetUserAsync(httpContext.User);
            if (admin is null)
                return Results.Unauthorized();

            decimal companyPortion = await settingsRepository.GetCompanyPortionAsync(cancellationToken);

            logger.LogInformation(
                "Admin {AdminName} ({AdminId}) order-paying {OrderCount} for user {UserId} with portion {Portion}",
                admin.UserName,
                admin.Id,
                requestDto.OrderIds.Count,
                requestDto.UserId,
                companyPortion);

            DateTime paidAt = DateTime.Now;

            (int paidCount, decimal totalPaid) = await orderRepository.MarkOrderAsPaidAsync(
                requestDto.UserId,
                requestDto.OrderIds,
                paidAt,
                companyPortion,
                cancellationToken);

            logger.LogInformation(
                "Order payment completed by admin {AdminName}: {PaidCount} orders marked as paid, total {TotalPaid:C} for user {UserId}",
                admin.UserName,
                paidCount,
                totalPaid,
                requestDto.UserId);

            return Results.Ok(new OrderPayResponseDto(paidCount, totalPaid));
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error in order payment for user {UserId}: {ErrorMessage}",
                requestDto.UserId,
                ex.Message);
            throw;
        }
    }

    private static async Task<IResult> GetOrdersForPeriodAsync(
        string userId,
        DateTime? startDate,
        DateTime? endDate,
        Guid? supplierId,
        IMealOrderRepository orderRepository,
        IMapper mapper,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                "Admin retrieving all orders for user {UserId} - Period: {StartDate} to {EndDate}, SupplierId: {SupplierId}",
                userId,
                startDate?.ToString("yyyy-MM-dd") ?? "None",
                endDate?.ToString("yyyy-MM-dd") ?? "None",
                supplierId?.ToString() ?? "All");

            IReadOnlyList<UserOrderPaymentItem> items =
                await orderRepository.GetAllOrdersForPeriodAsync(userId, startDate, endDate, supplierId, cancellationToken);

            List<UserOrderPaymentItemDto> dtos = items.Select(mapper.Map<UserOrderPaymentItemDto>).ToList();

            logger.LogInformation(
                "Admin retrieved {TotalCount} orders for user {UserId} ({PaidCount} paid, {UnpaidCount} unpaid)",
                dtos.Count,
                userId,
                dtos.Count(d => d.PaymentStatus == Shared.Common.Enums.PaymentStatusDto.Paid),
                dtos.Count(d => d.PaymentStatus == Shared.Common.Enums.PaymentStatusDto.Unpaid));

            return Results.Ok(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error retrieving period orders for user {UserId}: {ErrorMessage}",
                userId,
                ex.Message);
            throw;
        }
    }
}
