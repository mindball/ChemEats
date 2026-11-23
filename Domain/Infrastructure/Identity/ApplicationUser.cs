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
    public const int AbbreviationMaxLength = 3;
    public const int FullNameMaxLength = 256;

    // Separate from the registration Email, which is set when the employee is created.
    public string? NotificationEmail { get; private set; }
    public bool OrderRemindersEnabled { get; private set; }

    public IReadOnlyCollection<MealOrder> Orders => _orders.AsReadOnly();

    public IReadOnlyCollection<Supplier> SupervisedSuppliers => _supervisedSuppliers.AsReadOnly();


    public static ApplicationUser CreateEmployee(string abbreviation, string fullName, string? email)
    {
        string code = NormalizeAbbreviation(abbreviation);
        if (code.Length == 0)
            throw new DomainException("Abbreviation is required.");
        if (code.Length > AbbreviationMaxLength)
            throw new DomainException($"Abbreviation must be at most {AbbreviationMaxLength} characters.");
        if (!code.All(char.IsLetterOrDigit))
            throw new DomainException("Abbreviation may contain only letters and digits.");

        string name = fullName?.Trim() ?? string.Empty;
        if (name.Length == 0)
            throw new DomainException("Full name is required.");
        if (name.Length > FullNameMaxLength)
            throw new DomainException($"Full name must be at most {FullNameMaxLength} characters.");

        string? normalizedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        if (normalizedEmail is not null && !IsValidEmail(normalizedEmail))
            throw new DomainException("Email is not a valid email address.");

        ApplicationUser user = new()
        {
            UserName = code,
            Email = normalizedEmail,
            EmailConfirmed = normalizedEmail is not null
        };
        user.SetProfile(name, code);
        return user;
    }

    public static string NormalizeAbbreviation(string? abbreviation) =>
        abbreviation?.Trim().ToUpperInvariant() ?? string.Empty;

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