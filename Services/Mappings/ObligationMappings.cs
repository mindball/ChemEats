using Domain.Entities;
using Domain.Models.Obligations;
using Mapster;
using Shared.DTOs.Obligations;

namespace Services.Mappings;

public class ObligationMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // PaymentStatus -> PaymentStatusDto is configured in OrderMappings.
        config.NewConfig<OtherObligationItem, OtherObligationDto>();
        config.NewConfig<ObligationType, ObligationTypeDto>();
    }
}
