namespace Domain.Models.Notifications;

public sealed record OrderReminderCandidate(
    string UserId,
    string EmployeeName,
    string NotificationEmail,
    Guid MenuId,
    DateTime MenuDate,
    DateTime ActiveUntil,
    string SupplierName
);
