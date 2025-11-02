using Domain.Entities;
using Mapster;
using Shared.DTOs.Orders;

namespace Services.Mappings;


using Mapster;


public class OrderMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<OrderRequestItemDto, MealOrder>()
            .MapWith(static src => CreateMealOrder(src));
    }

    private static MealOrder CreateMealOrder(OrderRequestItemDto src)
    {
        var context = MapContext.Current
                      ?? throw new InvalidOperationException("MapContext.Current is null. Did you forget to use AddParameters()?");

        if (!context.Parameters.TryGetValue("userId", out var userIdObj))
            throw new ArgumentException("Mapping parameter 'userId' is required.");

        var userId = userIdObj as string
                     ?? throw new ArgumentException("Mapping parameter 'userId' must be a string.");

        return MealOrder.Create(Guid.NewGuid(), userId, src.MealId, src.Date);
    }
}


