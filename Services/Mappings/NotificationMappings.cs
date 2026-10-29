using Domain.Infrastructure.Identity;
using Mapster;
using Shared.DTOs.Notifications;

namespace Services.Mappings;

public class NotificationMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ApplicationUser, NotificationSettingsDto>()
            .MapWith(src => new NotificationSettingsDto(src.NotificationEmail, src.OrderRemindersEnabled));
    }
}
