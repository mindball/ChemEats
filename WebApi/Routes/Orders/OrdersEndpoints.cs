using Domain.Entities;
using Domain.Infrastructure.Identity;
using Domain.Repositories.MealOrders;
using Domain.Repositories.Meals;
using Microsoft.AspNetCore.Identity;
using Shared.DTOs.Orders;
using WebApi.Infrastructure.Filters;

namespace WebApi.Routes.Orders;

public static class OrdersEndpoints
{
    public static void MapMealOrderEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/mealorders")
            .RequireAuthorization();

        group.MapPost("/", async (
                PlaceOrdersRequest request,
                IMealOrderRepository orderRepository,
                IMealRepository mealRepository,
                UserManager<ApplicationUser> userManager,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                if (request?.Items is null || request.Items.Count == 0)
                    return Results.BadRequest("Request must contain at least one item.");

                ApplicationUser? user = await userManager.GetUserAsync(httpContext.User);
                if (user == null)
                    return Results.Unauthorized();

                List<Guid> createdOrders = [];

                foreach (OrderRequestItemDto item in request.Items)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (item.Quantity < 1)
                        return Results.BadRequest("Quantity must be at least 1.");

                    Meal? meal = await mealRepository.GetByIdAsync(item.MealId, cancellationToken);
                    if (meal is null)
                        return Results.BadRequest($"Meal with id '{item.MealId}' does not exist.");

                    for (int i = 0; i < item.Quantity; i++)
                    {
                        MealOrder order = MealOrder.Create(
                            Guid.NewGuid(),
                            user.Id,
                            item.MealId,
                            item.Date);

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
                    order.Date,
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
    }
}