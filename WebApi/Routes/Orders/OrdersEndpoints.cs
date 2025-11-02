using Domain.Entities;
using Domain.Infrastructure.Identity;
using Domain.Repositories.MealOrders;
using Domain.Repositories.Meals;
using Mapster;
using MapsterMapper;
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
                PlaceOrdersRequestDto requestDto,
                IMealOrderRepository orderRepository,
                IMealRepository mealRepository,
                UserManager<ApplicationUser> userManager,
                HttpContext httpContext,
                IMapper mapper,
                CancellationToken cancellationToken) =>
            {
                if (requestDto?.Items is null || requestDto.Items.Count == 0)
                    return Results.BadRequest("Request must contain at least one item.");

                ApplicationUser? user = await userManager.GetUserAsync(httpContext.User);
                if (user == null)
                    return Results.Unauthorized();

                List<Guid> createdOrders = new();

                foreach (OrderRequestItemDto item in requestDto.Items)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (item.Quantity < 1)
                        return Results.BadRequest("Quantity must be at least 1.");

                    Meal? meal = await mealRepository.GetByIdAsync(item.MealId, cancellationToken);
                    if (meal is null)
                        return Results.BadRequest($"Meal with id '{item.MealId}' does not exist.");

                    for (int i = 0; i < item.Quantity; i++)
                    {
                        MealOrder order = mapper.From(item)
                            .AddParameters("userId", user.Id)
                            .AdaptToType<MealOrder>();

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
                    Date = order.Date, // DateOnly will be serialized by JSON as date
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