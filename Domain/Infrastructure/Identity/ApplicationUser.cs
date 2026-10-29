using System.Net.Mail;
using Domain.Entities;
using Domain.Infrastructure.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace Domain.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    private readonly List<MealOrder> _orders = [];
    private readonly List<Supplier> _supervisedSuppliers = [];

    public string Abbreviation { get; private set; } = null!;
    public string FullName { get; private set; } = null!;

    public const int NotificationEmailMaxLength = 256;

    // Separate from the registration Email, which is owned by the HR employee sync.
    public string? NotificationEmail { get; private set; }
    public bool OrderRemindersEnabled { get; private set; }

    public IReadOnlyCollection<MealOrder> Orders => _orders.AsReadOnly();

    public IReadOnlyCollection<Supplier> SupervisedSuppliers => _supervisedSuppliers.AsReadOnly();


    public void AddOrder(MealOrder order)
    {
        if (order is null) throw new ArgumentNullException(nameof(order));
        _orders.Add(order);
    }

    public void SetProfile(string fullName, string abbreviation)
    {
        FullName = fullName;
        Abbreviation = abbreviation;
    }

    public void UpdateNotificationSettings(string? notificationEmail, bool orderRemindersEnabled)
    {
        string? email = string.IsNullOrWhiteSpace(notificationEmail) ? null : notificationEmail.Trim();

        if (email is not null && !IsValidEmail(email))
            throw new DomainException("Notification email is not a valid email address.");

        if (orderRemindersEnabled && email is null)
            throw new DomainException("A notification email is required to enable order reminders.");

        NotificationEmail = email;
        OrderRemindersEnabled = orderRemindersEnabled;
    }

    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > NotificationEmailMaxLength)
            return false;

        // MailAddress also accepts "Name <address>" forms; only a bare address is allowed here.
        return MailAddress.TryCreate(email, out MailAddress? address)
               && string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase);
    }
}