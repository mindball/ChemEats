using Domain.Models.Orders;
using Mapster;
using Shared.DTOs.Reports;

namespace Services.Mappings;

public class MenuReportMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<UserOrderItem, MenuReportOrderDto>();
    }
}
