using Domain.Entities;
using Domain.Models.Obligations;
using Domain.Models.Orders;
using Mapster;
using MapsterMapper;
using Services.Mappings;
using Shared.Common.Enums;
using Shared.DTOs.Obligations;
using Shared.DTOs.Orders;

namespace ChemEats.Tests.Services.Mappings;

public class ObligationMappingsTests
{
    [Fact]
    public void Map_WhenMappingObligationItem_ShouldCopyFieldsAndMapPaymentStatus()
    {
        IMapper mapper = CreateMapper();
        OtherObligationItem item = new(Guid.NewGuid(), "user-1", "User One", Guid.NewGuid(), "Printer toner", 12.5m, new DateTime(2026, 10, 5),
            new DateTime(2026, 10, 5, 9, 0, 0), "Admin User", PaymentStatus.Paid, new DateTime(2026, 10, 31), "Cashier");

        OtherObligationDto dto = mapper.Map<OtherObligationDto>(item);

        Assert.Equal(item.Id, dto.Id);
        Assert.Equal("User One", dto.EmployeeName);
        Assert.Equal(item.ObligationTypeId, dto.ObligationTypeId);
        Assert.Equal("Printer toner", dto.Description);
        Assert.Equal(12.5m, dto.Amount);
        Assert.Equal(item.Date, dto.Date);
        Assert.Equal("Admin User", dto.CreatedByName);
        Assert.Equal(PaymentStatusDto.Paid, dto.PaymentStatus);
        Assert.Equal(item.PaidOn, dto.PaidOn);
        Assert.Equal("Cashier", dto.PaidByName);
    }

    [Fact]
    public void Map_WhenMappingObligationType_ShouldCopyIdAndName()
    {
        IMapper mapper = CreateMapper();
        ObligationType type = ObligationType.Create("Printer toner");

        ObligationTypeDto dto = mapper.Map<ObligationTypeDto>(type);

        Assert.Equal(new ObligationTypeDto(type.Id, "Printer toner"), dto);
    }

    [Fact]
    public void Map_WhenMappingMealOutstandingSummary_ShouldLeaveOtherObligationFieldsAtZero()
    {
        IMapper mapper = CreateMapper();
        EmployeeOutstandingSummary summary = new("user-1", "User One", 2, 20m, 3m, 17m, 1, 5m);

        EmployeeOutstandingSummaryDto dto = mapper.Map<EmployeeOutstandingSummaryDto>(summary);

        Assert.Equal(17m, dto.NetAmount);
        Assert.Equal(5m, dto.EarlierNetAmount);
        Assert.Equal(0, dto.OtherUnpaidCount);
        Assert.Equal(0m, dto.OtherAmount);
    }

    private static IMapper CreateMapper()
    {
        TypeAdapterConfig config = new();
        config.Scan(typeof(ObligationMappings).Assembly);
        return new Mapper(config);
    }
}
