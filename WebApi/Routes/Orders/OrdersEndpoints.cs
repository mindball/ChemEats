using System.Diagnostics;
using Domain.Entities;
using Domain.Infrastructure.Identity;
using Domain.Models.Orders;
using Domain.Repositories.MealOrders;
using Domain.Repositories.Meals;
using Domain.Repositories.Settings;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Shared.DTOs.Orders;
using WebApi.Infrastructure.Filters;

namespace WebApi.Routes.Orders;

public static class OrdersEndpoints
{
    public static void MapMealOrderEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(ApiRoutes.Orders.Base)
            .RequireAuthorization();

        group.MapPost("/", PlaceOrdersAsync)
            .RequireAuthorization()
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
        group.MapGet("/{orderId:guid}", GetOrderByIdAsync)
            .RequireAuthorization()
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
        group.MapGet("/me", GetUserOrdersAsync)
            .RequireAuthorization()
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
        group.MapDelete("/{orderId:guid}", DeleteOrderAsync)
            .RequireAuthorization()
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
        group.MapGet("/me/items", GetUserOrderItemsAsync)
            .RequireAuthorization()
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
        group.MapGet("/me/payments", GetUserPaymentsAsync)
            .RequireAuthorization()
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
        group.MapGet("/me/payments/summary", GetUserPaymentSummaryAsync)
            .RequireAuthorization()
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
        group.MapPatch("/{orderId:guid}/pay", MarkOrderAsPaidAsync)
            .RequireAuthorization()
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
        group.MapGet("/me/menu/{menuId:guid}", GetUserOrdersByMenuAsync)
            .RequireAuthorization()
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
    }

    private static async Task<IResult> PlaceOrdersAsync(
        PlaceOrdersRequestDto requestDto,
        IMealOrderRepository orderRepository,
        IMealRepository mealRepository,
        ISettingsRepository settingsRepository,
        UserManager<ApplicationUser> userManager,
        HttpContext httpContext,
        IMapper mapper,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            if (requestDto?.Items is null || requestDto.Items.Count == 0)
            {
                logger.LogWarning("Place order request rejected - empty items list");
                return Results.BadRequest("Request must contain at least one item.");
            }

            ApplicationUser? user = await userManager.GetUserAsync(httpContext.User);
            if (user is null)
            {
                logger.LogWarning("Place order request rejected - unauthorized user");
                return Results.Unauthorized();
            }

            logger.LogInformation(
                "User {UserName} ({UserId}) placing order with {ItemCount} items",
                user.UserName,
                user.Id,
                requestDto.Items.Count);

            decimal companyPortion = await settingsRepository.GetCompanyPortionAsync(cancellationToken);
            logger.LogInformation("Company portion loaded: {CompanyPortion}", companyPortion);

            List<Guid> createdOrders = new();
            HashSet<DateOnly> portionAppliedForDate = new();
            int totalQuantity = 0;

            foreach (OrderRequestItemDto item in requestDto.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (item.Quantity < 1)
                {
                    logger.LogWarning(
                        "Invalid quantity {Quantity} for meal {MealId} by user {UserId}",
                        item.Quantity,
                        item.MealId,
                        user.Id);
                    return Results.BadRequest("Quantity must be at least 1.");
                }

                Meal? meal = await mealRepository.GetByIdAsync(item.MealId, cancellationToken);
                if (meal is null)
                {
                    logger.LogWarning(
                        "Meal {MealId} not found for user {UserId} order",
                        item.MealId,
                        user.Id);
                    return Results.BadRequest($"Meal with id '{item.MealId}' does not exist.");
                }

                logger.LogInformation(
                    "Processing order item: Meal {MealId} ({MealName}), Quantity {Quantity}, Date at {OrderedAt}, Price {Price}",
                    meal.Id,
                    meal.Name,
                    item.Quantity,
                    item.OrderedAt,
                    meal.Price.Amount);

                DateOnly dateOnly = DateOnly.FromDateTime(item.OrderedAt);

                bool alreadyAppliedToday = await orderRepository.HasPortionAppliedOnDateAsync(
                    user.Id, dateOnly, cancellationToken);

                bool shouldApplyPortion = !alreadyAppliedToday && !portionAppliedForDate.Contains(dateOnly);

                if (shouldApplyPortion && companyPortion > 0m)
                {
                    logger.LogInformation(
                        "Company portion {CompanyPortion} will be applied for user {UserId} on {Date}",
                        companyPortion,
                        user.Id,
                        dateOnly);
                }

                for (int i = 0; i < item.Quantity; i++)
                {
                    MealOrder order = mapper.From(item)
                        .AddParameters("userId", user.Id)
                        .AdaptToType<MealOrder>();

                    order.SetPriceSnapshot(meal.Price.Amount);

                    if (shouldApplyPortion && companyPortion > 0m)
                    {
                        order.ApplyPortion(companyPortion);
                        portionAppliedForDate.Add(dateOnly);
                        logger.LogInformation(
                            "Company portion applied to order {OrderId} for user {UserId} on {Date}",
                            order.Id,
                            user.Id,
                            dateOnly);
                        shouldApplyPortion = false;
                    }

                    await orderRepository.AddAsync(order, cancellationToken);
                    createdOrders.Add(order.Id);
                    totalQuantity++;
                }
            }

            logger.LogInformation(
                "Order placement completed: User {UserName} ({UserId}) created {TotalOrders} orders (total quantity: {TotalQuantity})",
                user.UserName,
                user.Id,
                createdOrders.Count,
                totalQuantity);

            return Results.Ok(new { Created = createdOrders.Count, Ids = createdOrders });
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error placing orders for user {UserName}: {ErrorMessage}",
                httpContext.User.Identity?.Name,
                ex.Message);
            throw;
        }
    }

    private static async Task<IResult> GetOrderByIdAsync(
        Guid orderId,
        IMealOrderRepository orderRepository,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Retrieving order by ID: {OrderId}", orderId);

            MealOrder? order = await orderRepository.GetByIdAsync(orderId, cancellationToken);
            if (order is null)
            {
                logger.LogWarning("Order {OrderId} not found", orderId);
                return Results.NotFound();
            }

            logger.LogInformation(
                "Order {OrderId} retrieved successfully: UserId {UserId}, MenuDateMealId {MealId}, OrderedAt {OrderedAt}, Status {Status}",
                order.Id,
                order.UserId,
                order.MealId,
                order.OrderedAt,
                order.Status);

            var dto = new
            {
                order.Id,
                order.UserId,
                order.MealId,
                order.OrderedAt,
                Status = order.Status.ToString(),
                Meal = order.Meal is null
                    ? null
                    : new
                    {
                        order.Meal.Id,
                        order.Meal.Name,
                        Price = order.Meal.Price.Amount
                    }
            };

            return Results.Ok(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error retrieving order {OrderId}: {ErrorMessage}",
                orderId,
                ex.Message);
            throw;
        }
    }

    private static async Task<IResult> GetUserOrdersAsync(
        Guid? supplierId,
        DateTime? startDate,
        DateTime? endDate,
        IMealOrderRepository orderRepository,
        UserManager<ApplicationUser> userManager,
        HttpContext httpContext,
        IMapper mapper,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            ApplicationUser? user = await userManager.GetUserAsync(httpContext.User);
            if (user == null)
            {
                logger.LogWarning("Get user orders request rejected - unauthorized");
                return Results.Unauthorized();
            }

            logger.LogInformation(
                "Retrieving orders for user {UserName} ({UserId}) - SupplierId: {SupplierId}, StartDate: {StartDate}, EndDate: {EndDate}",
                user.UserName,
                user.Id,
                supplierId?.ToString() ?? "All",
                startDate?.ToString("yyyy-MM-dd") ?? "None",
                endDate?.ToString("yyyy-MM-dd") ?? "None");

            Stopwatch sw = Stopwatch.StartNew();

            IReadOnlyList<UserOrderSummary> orders =
                await orderRepository.GetUserOrdersAsync(user.Id, supplierId, startDate, endDate,
                    cancellationToken);

            List<UserOrderDto> dtos = orders.Select(o => mapper.Map<UserOrderDto>(o)).ToList();

            sw.Stop();
            logger.LogInformation(
                "Retrieved {OrderCount} order summaries for user {UserId} in {ElapsedMs} ms",
                dtos.Count,
                user.Id,
                sw.ElapsedMilliseconds);

            return Results.Ok(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error retrieving user orders: {ErrorMessage}",
                ex.Message);
            throw;
        }
    }

    private static async Task<IResult> DeleteOrderAsync(
        Guid orderId,
        IMealOrderRepository orderRepository,
        UserManager<ApplicationUser> userManager,
        HttpContext httpContext,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            ApplicationUser? user = await userManager.GetUserAsync(httpContext.User);
            if (user == null)
            {
                logger.LogWarning("Delete order {OrderId} request rejected - unauthorized", orderId);
                return Results.Unauthorized();
            }

            logger.LogWarning(
                "User {UserName} ({UserId}) attempting to delete order {OrderId}",
                user.UserName,
                user.Id,
                orderId);

            MealOrder? order = await orderRepository.GetByIdAsync(orderId, cancellationToken);
            if (order is null)
            {
                logger.LogWarning(
                    "Order {OrderId} not found for deletion by user {UserId}",
                    orderId,
                    user.Id);
                return Results.NotFound();
            }

            if (!string.Equals(order.UserId, user.Id, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning(
                    "User {UserId} forbidden to delete order {OrderId} (belongs to user {OrderUserId})",
                    user.Id,
                    orderId,
                    order.UserId);
                return Results.Forbid();
            }

            await orderRepository.SoftDeleteAsync(orderId, cancellationToken);

            logger.LogInformation(
                "Order {OrderId} successfully deleted by user {UserName} ({UserId})",
                orderId,
                user.UserName,
                user.Id);

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error deleting order {OrderId}: {ErrorMessage}",
                orderId,
                ex.Message);
            throw;
        }
    }

    private static async Task<IResult> GetUserOrderItemsAsync(
        Guid? supplierId,
        DateTime? startDate,
        DateTime? endDate,
        [FromQuery] bool includeDeleted,
        [FromQuery] string? status,
        IMealOrderRepository orderRepository,
        UserManager<ApplicationUser> userManager,
        HttpContext httpContext,
        IMapper mapper,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            ApplicationUser? user = await userManager.GetUserAsync(httpContext.User);
            if (user == null)
            {
                logger.LogWarning("Get user order items request rejected - unauthorized");
                return Results.Unauthorized();
            }

            MealOrderStatus? orderStatus = null;
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<MealOrderStatus>(status, true, out MealOrderStatus parsedStatus))
            {
                orderStatus = parsedStatus;
            }

            logger.LogInformation(
                "Retrieving order items for user {UserName} ({UserId}) - SupplierId: {SupplierId}, StartDate: {StartDate}, EndDate: {EndDate}, IncludeDeleted: {IncludeDeleted}, Status: {Status}",
                user.UserName,
                user.Id,
                supplierId?.ToString() ?? "All",
                startDate?.ToString("yyyy-MM-dd") ?? "None",
                endDate?.ToString("yyyy-MM-dd") ?? "None",
                includeDeleted,
                orderStatus?.ToString() ?? "All");

            Stopwatch sw = Stopwatch.StartNew();

            IReadOnlyList<UserOrderItem> items =
                await orderRepository.GetUserOrderItemsAsync(user.Id, supplierId, startDate, endDate,
                    includeDeleted,
                    orderStatus,
                    cancellationToken);

            List<UserOrderItemDto> userOrderItems = items.Select(mapper.Map<UserOrderItemDto>).ToList();

            sw.Stop();
            logger.LogInformation(
                "Retrieved {ItemCount} order items for user {UserId} in {ElapsedMs} ms",
                userOrderItems.Count,
                user.Id,
                sw.ElapsedMilliseconds);

            return Results.Ok(userOrderItems);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error retrieving user order items: {ErrorMessage}",
                ex.Message);
            throw;
        }
    }

    private static async Task<IResult> GetUserPaymentsAsync(
        Guid? supplierId,
        IMealOrderRepository orderRepository,
        UserManager<ApplicationUser> userManager,
        HttpContext httpContext,
        IMapper mapper,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            ApplicationUser? user = await userManager.GetUserAsync(httpContext.User);
            if (user == null)
            {
                logger.LogWarning("Get unpaid orders request rejected - unauthorized");
                return Results.Unauthorized();
            }

            logger.LogInformation(
                "Retrieving unpaid orders for user {UserName} ({UserId}) - SupplierId: {SupplierId}",
                user.UserName,
                user.Id,
                supplierId?.ToString() ?? "All");

            Stopwatch sw = Stopwatch.StartNew();

            IReadOnlyList<UserOrderPaymentItem> items =
                await orderRepository.GetUnpaidOrdersAsync(
                    user.Id,
                    supplierId,
                    cancellationToken);

            List<UserOrderPaymentItemDto> dtos = items.Select(mapper.Map<UserOrderPaymentItemDto>).ToList();
            decimal totalAmount = dtos.Sum(d => d.PortionAmount);

            sw.Stop();
            logger.LogInformation(
                "Retrieved {UnpaidOrderCount} unpaid orders for user {UserId} (Total: {TotalAmount:C}) in {ElapsedMs} ms",
                dtos.Count,
                user.Id,
                totalAmount,
                sw.ElapsedMilliseconds);

            return Results.Ok(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error retrieving unpaid orders: {ErrorMessage}",
                ex.Message);
            throw;
        }
    }

    private static async Task<IResult> GetUserPaymentSummaryAsync(
        IMealOrderRepository orderRepository,
        UserManager<ApplicationUser> userManager,
        HttpContext httpContext,
        IMapper mapper,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            ApplicationUser? user = await userManager.GetUserAsync(httpContext.User);
            if (user == null)
            {
                logger.LogWarning("Get outstanding summary request rejected - unauthorized");
                return Results.Unauthorized();
            }

            logger.LogInformation(
                "Retrieving outstanding payment summary for user {UserName} ({UserId})",
                user.UserName,
                user.Id);

            UserOutstandingSummary summary =
                await orderRepository.GetUserOutstandingSummaryAsync(
                    user.Id,
                    cancellationToken);

            UserOutstandingSummaryDto dto = mapper.Map<UserOutstandingSummaryDto>(summary);

            logger.LogInformation(
                "Outstanding summary for user {UserId}: TotalOutstanding: {TotalOutstanding:C}",
                user.Id,
                dto.TotalOutstanding);

            return Results.Ok(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error retrieving outstanding summary: {ErrorMessage}",
                ex.Message);
            throw;
        }
    }

    private static async Task<IResult> MarkOrderAsPaidAsync(
        Guid orderId,
        IMealOrderRepository orderRepository,
        UserManager<ApplicationUser> userManager,
        HttpContext httpContext,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            ApplicationUser? user = await userManager.GetUserAsync(httpContext.User);
            if (user == null)
            {
                logger.LogWarning("Mark as paid request for order {OrderId} rejected - unauthorized", orderId);
                return Results.Unauthorized();
            }

            logger.LogInformation(
                "User {UserName} ({UserId}) marking order {OrderId} as paid",
                user.UserName,
                user.Id,
                orderId);

            DateTime paidAt = DateTime.Now;
            await orderRepository.MarkAsPaidAsync(
                orderId,
                user.Id,
                paidAt,
                cancellationToken);

            logger.LogInformation(
                "Order {OrderId} successfully marked as paid by user {UserId} at {PaidAt}",
                orderId,
                user.Id,
                paidAt);

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error marking order {OrderId} as paid: {ErrorMessage}",
                orderId,
                ex.Message);
            throw;
        }
    }

    private static async Task<IResult> GetUserOrdersByMenuAsync(
        Guid menuId,
        IMealOrderRepository orderRepository,
        UserManager<ApplicationUser> userManager,
        HttpContext httpContext,
        IMapper mapper,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            ApplicationUser? user = await userManager.GetUserAsync(httpContext.User);
            if (user is null)
            {
                logger.LogWarning("Get orders by menu {MenuId} request rejected - unauthorized", menuId);
                return Results.Unauthorized();
            }

            logger.LogInformation(
                "Retrieving orders for user {UserName} ({UserId}) on menu {MenuId}",
                user.UserName,
                user.Id,
                menuId);

            IReadOnlyList<UserOrderItem> items =
                await orderRepository.GetOrdersByMenuAsync(user.Id, menuId, cancellationToken);

            List<UserOrderItemDto> dtos = items.Select(mapper.Map<UserOrderItemDto>).ToList();

            logger.LogInformation(
                "Retrieved {ItemCount} orders for user {UserId} on menu {MenuId}",
                dtos.Count,
                user.Id,
                menuId);

            return Results.Ok(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error retrieving orders for menu {MenuId}: {ErrorMessage}",
                menuId,
                ex.Message);
            throw;
        }
    }
}
