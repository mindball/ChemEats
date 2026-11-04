using Domain.Infrastructure.Identity;
using Domain.Models.Obligations;
using Domain.Models.Orders;
using Domain.Repositories.MealOrders;
using Domain.Repositories.Obligations;
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

        group.MapGet(ApiRoutes.AdminOrders.UnpaidRoute, GetUnpaidOrdersAsync)
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
        group.MapPost(ApiRoutes.AdminOrders.OrderPay, OrderPayAsync)
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
        group.MapPost(ApiRoutes.AdminOrders.PayAll, PayAllUnpaidAsync)
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
        group.MapGet(ApiRoutes.AdminOrders.PeriodRoute, GetOrdersForPeriodAsync)
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
        group.MapGet(ApiRoutes.AdminOrders.Outstanding, GetOutstandingSummaryAsync)
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
    }

    private static async Task<IResult> GetOutstandingSummaryAsync(
        DateTime? startDate,
        DateTime? endDate,
        string? userId,
        bool? allDates,
        IMealOrderRepository orderRepository,
        IOtherObligationRepository obligationRepository,
        ISettingsRepository settingsRepository,
        IMapper mapper,
        UserManager<ApplicationUser> userManager,
        HttpContext httpContext,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            ApplicationUser? admin = await userManager.GetUserAsync(httpContext.User);
            if (admin is null)
                return Results.Unauthorized();

            if (allDates == true)
            {
                startDate = null;
                endDate = null;
            }
            else if (!startDate.HasValue || !endDate.HasValue)
            {
                logger.LogWarning("Outstanding summary rejected - start and end date are required");
                return Results.BadRequest("Start date and end date are required.");
            }
            else if (endDate.Value.Date < startDate.Value.Date)
            {
                logger.LogWarning(
                    "Outstanding summary rejected - end date {EndDate} is before start date {StartDate}",
                    endDate.Value.ToString("yyyy-MM-dd"),
                    startDate.Value.ToString("yyyy-MM-dd"));
                return Results.BadRequest("End date cannot be before start date.");
            }

            decimal companyPortion = await settingsRepository.GetCompanyPortionAsync(cancellationToken);

            Stopwatch stopwatch = Stopwatch.StartNew();

            string? userFilter = string.IsNullOrWhiteSpace(userId) ? null : userId;

            IReadOnlyList<EmployeeOutstandingSummary> summaries = await orderRepository.GetOutstandingSummariesAsync(
                startDate,
                endDate,
                companyPortion,
                userFilter,
                cancellationToken);

            IReadOnlyList<EmployeeObligationSummary> obligationSummaries =
                await obligationRepository.GetOutstandingSummariesAsync(endDate, userFilter, cancellationToken);

            List<EmployeeOutstandingSummaryDto> dtos = MergeObligations(
                summaries.Select(mapper.Map<EmployeeOutstandingSummaryDto>).ToList(),
                obligationSummaries);

            stopwatch.Stop();
            logger.LogInformation(
                "Admin {AdminName} ({AdminId}) retrieved outstanding summary for {EmployeeCount} employees - Period: {StartDate} to {EndDate}, Portion: {Portion} in {ElapsedMs} ms",
                admin.UserName,
                admin.Id,
                dtos.Count,
                startDate?.ToString("yyyy-MM-dd") ?? "all",
                endDate?.ToString("yyyy-MM-dd") ?? "all",
                companyPortion,
                stopwatch.ElapsedMilliseconds);

            return Results.Ok(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error retrieving outstanding summary - Period: {StartDate} to {EndDate}: {ErrorMessage}",
                startDate?.ToString("yyyy-MM-dd") ?? "None",
                endDate?.ToString("yyyy-MM-dd") ?? "None",
                ex.Message);
            throw;
        }
    }

    // Other obligations are paid separately from meals, but shown in the same summary;
    // employees who only owe other obligations get a row with no meal amounts.
    private static List<EmployeeOutstandingSummaryDto> MergeObligations(
        List<EmployeeOutstandingSummaryDto> mealSummaries,
        IReadOnlyList<EmployeeObligationSummary> obligationSummaries)
    {
        if (obligationSummaries.Count == 0)
            return mealSummaries;

        Dictionary<string, EmployeeObligationSummary> obligationsByUser =
            obligationSummaries.ToDictionary(s => s.UserId, StringComparer.Ordinal);

        List<EmployeeOutstandingSummaryDto> merged = mealSummaries
            .Select(summary => obligationsByUser.TryGetValue(summary.UserId, out EmployeeObligationSummary? other)
                ? summary with { OtherUnpaidCount = other.UnpaidCount, OtherAmount = other.Amount }
                : summary)
            .ToList();

        HashSet<string> mealUserIds = mealSummaries.Select(s => s.UserId).ToHashSet(StringComparer.Ordinal);

        merged.AddRange(obligationSummaries
            .Where(other => !mealUserIds.Contains(other.UserId))
            .Select(other => new EmployeeOutstandingSummaryDto(
                other.UserId,
                other.EmployeeName,
                0,
                0m,
                0m,
                0m,
                OtherUnpaidCount: other.UnpaidCount,
                OtherAmount: other.Amount)));

        return merged
            .OrderBy(s => s.EmployeeName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(s => s.UserId, StringComparer.Ordinal)
            .ToList();
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
                admin.Id,
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

    private static async Task<IResult> PayAllUnpaidAsync(
        PayAllUnpaidRequestDto requestDto,
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

            if (string.IsNullOrWhiteSpace(requestDto.UserId))
            {
                logger.LogWarning("Pay all rejected - user ID is required");
                return Results.BadRequest("User ID is required.");
            }

            ApplicationUser? admin = await userManager.GetUserAsync(httpContext.User);
            if (admin is null)
                return Results.Unauthorized();

            decimal companyPortion = await settingsRepository.GetCompanyPortionAsync(cancellationToken);

            (int paidCount, decimal totalPaid) = await orderRepository.MarkAllUnpaidAsPaidAsync(
                requestDto.UserId,
                requestDto.UpToDate,
                DateTime.Now,
                companyPortion,
                admin.Id,
                cancellationToken);

            logger.LogInformation(
                "Admin {AdminName} ({AdminId}) paid all unpaid orders up to {UpToDate} for user {UserId}: {PaidCount} orders, total {TotalPaid}",
                admin.UserName,
                admin.Id,
                requestDto.UpToDate?.ToString("yyyy-MM-dd") ?? "any date",
                requestDto.UserId,
                paidCount,
                totalPaid);

            return Results.Ok(new OrderPayResponseDto(paidCount, totalPaid));
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error paying all unpaid orders for user {UserId}: {ErrorMessage}",
                requestDto?.UserId,
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
