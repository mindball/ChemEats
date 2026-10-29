using Domain.Models.Orders;
using Mapster;
using MapsterMapper;
using Services.Mappings;
using Shared.DTOs.Orders;

namespace ChemEats.Tests.Services.Mappings;

public class OrderMappingsTests
{
    [Fact]
    public void Map_WhenMappingUserOrderItem_ShouldCopyFieldsIncludingPaymentAndLeaveExpectedPortionAtZero()
    {
        TypeAdapterConfig config = new();
        config.Scan(typeof(OrderMappings).Assembly);
        IMapper mapper = new Mapper(config);

        UserOrderItem item = new(Guid.NewGuid(), "user-1", "User One", Guid.NewGuid(), "Soup", Guid.NewGuid(), "Supplier",
            DateTime.Now, DateTime.Today.AddDays(1), 8m, "Completed", true, false, 2m, 6m, true, DateTime.Today);

        UserOrderItemDto dto = mapper.Map<UserOrderItemDto>(item);

        Assert.Equal(item.OrderId, dto.OrderId);
        Assert.Equal(item.MenuDate, dto.MenuDate);
        Assert.True(dto.PortionApplied);
        Assert.Equal(2m, dto.PortionAmount);
        Assert.Equal(6m, dto.NetAmount);
        Assert.True(dto.IsPaid);
        Assert.Equal(DateTime.Today, dto.PaidOn);
        Assert.Equal(0m, dto.ExpectedPortionAmount);
    }
}
