using Domain.Entities;
using Domain.Infrastructure.Identity;
using Domain.Models.Orders;
using Domain.Repositories.MealOrders;
using Domain.Repositories.Meals;
using Domain.Repositories.Settings;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Shared.DTOs.Orders;
using System;
using Microsoft.AspNetCore.Mvc;
using WebApi.Infrastructure.Filters;

namespace WebApi.Routes.Orders;

public static class OrdersEndpoints
{
    public static void MapMealOrderEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/mealorders")
            .RequireAuthorization();

        group.MapPost("/", async (
        PlaceOrdersRequestDto requestDto,
        IMealOrderRepository orderRepository,
        IMealRepository mealRepository,
        ISettingsRepository settingsRepository,
        UserManager<ApplicationUser> userManager,
        HttpContext httpContext,
        IMapper mapper,
        CancellationToken cancellationToken) =>
        {
            if (requestDto?.Items is null || requestDto.Items.Count == 0)
                return Results.BadRequest("Request must contain at least one item.");

            ApplicationUser? user = await userManager.GetUserAsync(httpContext.User);
            if (user is null)
                return Results.Unauthorized();

            decimal companyPortion = await settingsRepository.GetCompanyPortionAsync(cancellationToken);

            var createdOrders = new List<Guid>();

            // Track portion application per date within this request
            var portionAppliedForDate = new HashSet<DateOnly>();

            foreach (OrderRequestItemDto item in requestDto.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (item.Quantity < 1)
                    return Results.BadRequest("Quantity must be at least 1.");

                Meal? meal = await mealRepository.GetByIdAsync(item.MealId, cancellationToken);
                if (meal is null)
                    return Results.BadRequest($"Meal with id '{item.MealId}' does not exist.");

                DateOnly dateOnly = DateOnly.FromDateTime(item.Date);

                // Check if the user already consumed a portion earlier that day (across all suppliers/menus)
                bool alreadyAppliedToday = await orderRepository.HasPortionAppliedOnDateAsync(
                    user.Id, dateOnly, cancellationToken);

                // Apply portion once per date within this request if not consumed yet
                bool shouldApplyPortion = !alreadyAppliedToday && !portionAppliedForDate.Contains(dateOnly);

                for (int i = 0; i < item.Quantity; i++)
                {
                    MealOrder order = mapper.From(item)
                        .AddParameters("userId", user.Id)
                        .AdaptToType<MealOrder>();

                    // Snapshot current meal price
                    order.SetPriceSnapshot(meal.Price.Amount);

                    if (shouldApplyPortion && companyPortion > 0m)
                    {
                        order.ApplyPortion(companyPortion);
                        portionAppliedForDate.Add(dateOnly);
                        shouldApplyPortion = false;
                    }

                    await orderRepository.AddAsync(order, cancellationToken);
                    createdOrders.Add(order.Id);
                }
            }

            return Results.Ok(new { Created = createdOrders.Count, Ids = createdOrders });
        })
    .RequireAuthorization()
    .AddEndpointFilter<AuthorizedRequestLoggingFilter>();


        // GET single order by id
        group.MapGet("/{orderId:guid}", async (
                Guid orderId,
                IMealOrderRepository orderRepository,
                CancellationToken cancellationToken) =>
            {
                MealOrder? order = await orderRepository.GetByIdAsync(orderId, cancellationToken);
                if (order is null)
                    return Results.NotFound();

                var dto = new
                {
                    order.Id,
                    order.UserId,
                    order.MealId,
                    order.Date, // DateOnly will be serialized by JSON as date
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
            })
            .RequireAuthorization()
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();

        group.MapGet("/me", async (
                Guid? supplierId,
                DateTime? startDate,
                DateTime? endDate,
                IMealOrderRepository orderRepository,
                UserManager<ApplicationUser> userManager,
                HttpContext httpContext,
                IMapper mapper,
                CancellationToken cancellationToken) =>
            {
                ApplicationUser? user = await userManager.GetUserAsync(httpContext.User);
                if (user == null)
                    return Results.Unauthorized();

                IReadOnlyList<UserOrderSummary> orders =
                    await orderRepository.GetUserOrdersAsync(user.Id, supplierId, startDate, endDate,
                        cancellationToken);

                List<UserOrderDto> dtos = orders.Select(o => mapper.Map<UserOrderDto>(o)).ToList();

                return Results.Ok(dtos);
            })
            .RequireAuthorization()
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();

        group.MapDelete("/{orderId:guid}", async (
                Guid orderId,
                IMealOrderRepository orderRepository,
                UserManager<ApplicationUser> userManager,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                ApplicationUser? user = await userManager.GetUserAsync(httpContext.User);
                if (user == null)
                    return Results.Unauthorized();

                MealOrder? order = await orderRepository.GetByIdAsync(orderId, cancellationToken);
                if (order is null)
                    return Results.NotFound();

                if (!string.Equals(order.UserId, user.Id, StringComparison.OrdinalIgnoreCase))
                    return Results.Forbid();

                await orderRepository.SoftDeleteAsync(orderId, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization()
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();

        group.MapGet("/me/items", async (
                Guid? supplierId,
                DateTime? startDate,
                DateTime? endDate,
                [FromQuery] bool includeDeleted,
                IMealOrderRepository orderRepository,
                UserManager<ApplicationUser> userManager,
                HttpContext httpContext,
                IMapper mapper,
                CancellationToken cancellationToken) =>
            {
                ApplicationUser? user = await userManager.GetUserAsync(httpContext.User);
                if (user == null)
                    return Results.Unauthorized();

                IReadOnlyList<UserOrderItem> items =
                    await orderRepository.GetUserOrderItemsAsync(user.Id, supplierId, startDate, endDate, includeDeleted,
                        cancellationToken);
                List<UserOrderItemDto> userOrderItems = items.Select(mapper.Map<UserOrderItemDto>).ToList();
                return Results.Ok(userOrderItems);
            })
            .RequireAuthorization()
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();

        group.MapGet("/me/payments", async (
                Guid? supplierId,
                IMealOrderRepository orderRepository,
                UserManager<ApplicationUser> userManager,
                HttpContext httpContext,
                IMapper mapper,
                CancellationToken cancellationToken) =>
            {
                ApplicationUser? user = await userManager.GetUserAsync(httpContext.User);
                if (user == null)
                    return Results.Unauthorized();

                IReadOnlyList<UserOrderPaymentItem> items =
                    await orderRepository.GetUnpaidOrdersAsync(
                        user.Id,
                        supplierId,
                        cancellationToken);

                List<UserOrderPaymentItemDto> dtos = items.Select(mapper.Map<UserOrderPaymentItemDto>).ToList();
                return Results.Ok(dtos);
            })
            .RequireAuthorization()
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();

        group.MapGet("/me/payments/summary", async (
                IMealOrderRepository orderRepository,
                UserManager<ApplicationUser> userManager,
                HttpContext httpContext,
                IMapper mapper,
                CancellationToken cancellationToken) =>
            {
                ApplicationUser? user = await userManager.GetUserAsync(httpContext.User);
                if (user == null)
                    return Results.Unauthorized();

                UserOutstandingSummary summary =
                    await orderRepository.GetUserOutstandingSummaryAsync(
                        user.Id,
                        cancellationToken);

                UserOutstandingSummaryDto dto = mapper.Map<UserOutstandingSummaryDto>(summary);
                return Results.Ok(dto);
            })
            .RequireAuthorization()
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();

        group.MapPatch("/{orderId:guid}/pay", async (
                Guid orderId,
                IMealOrderRepository orderRepository,
                UserManager<ApplicationUser> userManager,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                ApplicationUser? user = await userManager.GetUserAsync(httpContext.User);
                if (user == null)
                    return Results.Unauthorized();

                await orderRepository.MarkAsPaidAsync(
                    orderId,
                    user.Id,
                    DateTime.UtcNow,
                    cancellationToken);

                return Results.NoContent();
            })
            .RequireAuthorization()
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
    }
}