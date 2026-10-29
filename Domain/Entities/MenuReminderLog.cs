using Domain.Infrastructure.Exceptions;

namespace Domain.Entities;

public class MenuReminderLog
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = null!;
    public Guid MenuId { get; private set; }
    public DateTime SentAt { get; private set; }

    private MenuReminderLog() { }

    private MenuReminderLog(Guid id, string userId, Guid menuId, DateTime sentAt)
    {
        Id = id;
        UserId = userId;
        MenuId = menuId;
        SentAt = sentAt;
    }

    public static MenuReminderLog Create(string userId, Guid menuId, DateTime sentAt)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("User ID is required for a reminder log.");

        if (menuId == Guid.Empty)
            throw new DomainException("Menu ID is required for a reminder log.");

        return new MenuReminderLog(Guid.NewGuid(), userId, menuId, sentAt);
    }
}
