using Domain.Models.Orders;
using Mapster;
using Shared.DTOs.Orders;

namespace Services.Mappings;

public class UserOrderMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<UserOrderSummary, UserOrderDto>();
    }
}