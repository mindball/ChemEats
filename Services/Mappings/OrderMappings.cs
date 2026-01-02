using Domain.Entities;
using Domain.Models.Orders;
using Mapster;
using Shared.Common.Enums;
using Shared.DTOs.Orders;

namespace Services.Mappings;


using Mapster;


public class OrderMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<OrderRequestItemDto, MealOrder>()
            .MapWith(static src => CreateMealOrder(src));

        config.NewConfig<PaymentStatus, PaymentStatusDto>()
            .MapWith(src => MapPaymentStatus(src));

        config.NewConfig<UserOrderPaymentItem, UserOrderPaymentItemDto>();
        config.NewConfig<UserOutstandingSummary, UserOutstandingSummaryDto>();
    }

    private static MealOrder CreateMealOrder(OrderRequestItemDto src)
    {
        var context = MapContext.Current
                      ?? throw new InvalidOperationException("MapContext.Current is null. Did you forget to use AddParameters()?");

        if (!context.Parameters.TryGetValue("userId", out var userIdObj))
            throw new ArgumentException("Mapping parameter 'userId' is required.");

        var userId = userIdObj as string
                     ?? throw new ArgumentException("Mapping parameter 'userId' must be a string.");

        return MealOrder.Create(userId, src.MealId, src.Date);
    }

    private static PaymentStatusDto MapPaymentStatus(PaymentStatus status)
        => status == PaymentStatus.Paid
            ? PaymentStatusDto.Paid
            : PaymentStatusDto.Unpaid;
}


