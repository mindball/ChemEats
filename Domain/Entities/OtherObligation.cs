using Domain.Infrastructure.Exceptions;

namespace Domain.Entities;

// A personal, non-food charge an employee owes (e.g. a purchase the admin made for them).
// Unlike MealOrder it has no menu lifecycle and never receives the company portion.
public class OtherObligation
{
    public const int DescriptionMaxLength = 300;

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = null!;

    // Null only for obligations created before expense types existed.
    public Guid? ObligationTypeId { get; private set; }

    // Snapshot of the expense type name at creation/update time, so renaming a type does not rewrite history.
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
        Guid obligationTypeId,
        string description,
        decimal amount,
        DateTime date,
        DateTime createdAt,
        string createdByUserId)
    {
        Id = id;
        UserId = userId;
        ObligationTypeId = obligationTypeId;
        Description = description;
        Amount = amount;
        Date = date;
        CreatedAt = createdAt;
        CreatedByUserId = createdByUserId;
        PaymentStatus = PaymentStatus.Unpaid;
    }

    public static OtherObligation Create(
        string userId,
        ObligationType obligationType,
        decimal amount,
        DateTime date,
        DateTime createdAt,
        string createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("User ID is required to create an obligation.");

        if (string.IsNullOrWhiteSpace(createdByUserId))
            throw new DomainException("The user who creates the obligation is required.");

        string description = ValidateType(obligationType);
        ValidateAmount(amount);

        return new OtherObligation(
            Guid.NewGuid(),
            userId,
            obligationType.Id,
            description,
            amount,
            date.Date,
            createdAt,
            createdByUserId);
    }

    public void Update(ObligationType obligationType, decimal amount, DateTime date)
    {
        if (IsDeleted)
            throw new DomainException("Deleted obligations cannot be changed.");

        if (PaymentStatus == PaymentStatus.Paid)
            throw new DomainException("Paid obligations cannot be changed.");

        string description = ValidateType(obligationType);
        ValidateAmount(amount);

        ObligationTypeId = obligationType.Id;
        Description = description;
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

    private static string ValidateType(ObligationType? obligationType)
    {
        if (obligationType is null)
            throw new DomainException("Expense type is required.");

        if (obligationType.IsDeleted)
            throw new DomainException("Deleted expense types cannot be used.");

        return obligationType.Name;
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0m)
            throw new DomainException("Amount must be greater than zero.");

        if (decimal.Round(amount, 2) != amount)
            throw new DomainException("Amount cannot have more than two decimal places.");
    }
}
