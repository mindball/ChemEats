using Domain.Infrastructure.Exceptions;

namespace Domain.Entities;

// A personal, non-food charge an employee owes (e.g. a purchase the admin made for them).
// Unlike MealOrder it has no menu lifecycle and never receives the company portion.
public class OtherObligation
{
    public const int DescriptionMaxLength = 300;

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = null!;

    public string Description { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public DateTime Date { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public string CreatedByUserId { get; private set; } = null!;

    public PaymentStatus PaymentStatus { get; private set; }
    public DateTime? PaidOn { get; private set; }
    public string? PaidByUserId { get; private set; }

    public bool IsDeleted { get; private set; }

    private OtherObligation() { }

    private OtherObligation(
        Guid id,
        string userId,
        string description,
        decimal amount,
        DateTime date,
        DateTime createdAt,
        string createdByUserId)
    {
        Id = id;
        UserId = userId;
        Description = description;
        Amount = amount;
        Date = date;
        CreatedAt = createdAt;
        CreatedByUserId = createdByUserId;
        PaymentStatus = PaymentStatus.Unpaid;
    }

    public static OtherObligation Create(
        string userId,
        string description,
        decimal amount,
        DateTime date,
        DateTime createdAt,
        string createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("User ID is required to create an obligation.");

        if (string.IsNullOrWhiteSpace(createdByUserId))
            throw new DomainException("The user who creates the obligation is required.");

        string normalizedDescription = ValidateDescription(description);
        ValidateAmount(amount);

        return new OtherObligation(
            Guid.NewGuid(),
            userId,
            normalizedDescription,
            amount,
            date.Date,
            createdAt,
            createdByUserId);
    }

    public void Update(string description, decimal amount, DateTime date)
    {
        if (IsDeleted)
            throw new DomainException("Deleted obligations cannot be changed.");

        if (PaymentStatus == PaymentStatus.Paid)
            throw new DomainException("Paid obligations cannot be changed.");

        string normalizedDescription = ValidateDescription(description);
        ValidateAmount(amount);

        Description = normalizedDescription;
        Amount = amount;
        Date = date.Date;
    }

    public void MarkAsPaid(DateTime paidOn, string paidByUserId)
    {
        if (string.IsNullOrWhiteSpace(paidByUserId))
            throw new DomainException("The user who marks the obligation as paid is required.");

        if (IsDeleted)
            throw new DomainException("Deleted obligations cannot be paid.");

        if (PaymentStatus == PaymentStatus.Paid)
            throw new DomainException("Obligation is already paid.");

        PaymentStatus = PaymentStatus.Paid;
        PaidOn = paidOn;
        PaidByUserId = paidByUserId;
    }

    public void SoftDelete()
    {
        if (IsDeleted)
            throw new DomainException("Obligation is already deleted.");

        if (PaymentStatus == PaymentStatus.Paid)
            throw new DomainException("Paid obligations cannot be deleted.");

        IsDeleted = true;
    }

    private static string ValidateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Description is required.");

        string trimmed = description.Trim();

        if (trimmed.Length > DescriptionMaxLength)
            throw new DomainException($"Description cannot be longer than {DescriptionMaxLength} characters.");

        return trimmed;
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0m)
            throw new DomainException("Amount must be greater than zero.");

        if (decimal.Round(amount, 2) != amount)
            throw new DomainException("Amount cannot have more than two decimal places.");
    }
}
