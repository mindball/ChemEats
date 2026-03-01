using Domain.Infrastructure.Exceptions;
using Domain.Infrastructure.Identity;

namespace Domain.Entities;

public enum MealOrderStatus
{
    Pending,
    Completed,
    Cancelled
}

public enum PaymentStatus
{
    Unpaid,        
    Paid         
}

public class MealOrder
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = null!;
    public ApplicationUser? User { get; private set; }

    public Guid MealId { get; private set; }
    public Meal Meal { get; private set; } = null!;

    public DateTime MenuDate { get; private set; }     
    public DateTime OrderedAt { get; private set; }    

    public MealOrderStatus Status { get; private set; }
     
    public PaymentStatus PaymentStatus { get; private set; }
    public DateTime? PaidOn { get; private set; }

    public bool IsDeleted { get; private set; }

    public decimal PriceAmount { get; private set; }

    public bool PortionApplied { get; private set; }
    public decimal PortionAmount { get; private set; }

    private MealOrder() { }

    private MealOrder(Guid id, string userId, Guid mealId, DateTime menuDate, decimal priceSnapshot)
    {
        Id = id;
        UserId = userId;
        MealId = mealId;
        MenuDate = menuDate;           
        OrderedAt = DateTime.Now;   

        Status = MealOrderStatus.Pending;
        PaymentStatus = PaymentStatus.Unpaid;

        PriceAmount = priceSnapshot;
        PortionApplied = false;
        PortionAmount = 0m;
    }

    public static MealOrder Create(
        string userId,
        Guid mealId,
        DateTime menuDate,
        decimal priceSnapshot)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("User ID is required to create an order.");

        if (mealId == Guid.Empty)
            throw new DomainException("Meal ID is required to create an order.");

        if (priceSnapshot < 0)
            throw new DomainException("Price snapshot cannot be negative.");

        return new MealOrder(Guid.NewGuid(), userId, mealId, menuDate, priceSnapshot);
    }

    public void SetPriceSnapshot(decimal priceAmount)
    {
        if (priceAmount < 0)
            throw new DomainException("Price amount cannot be negative.");

        PriceAmount = priceAmount;
    }

    public void ApplyPortion(decimal portionAmount)
    {
        if (portionAmount < 0)
            throw new DomainException("Portion amount cannot be negative.");

        if (portionAmount > PriceAmount)
            throw new DomainException("Portion cannot exceed price.");

        PortionApplied = portionAmount > 0m;
        PortionAmount = portionAmount;
    }

    public decimal GetNetAmount() =>
        Math.Max(0m, PriceAmount - (PortionApplied ? PortionAmount : 0m));

    public void MarkAsPaid(DateTime paidOn)
    {
        if (Status == MealOrderStatus.Cancelled)
            throw new DomainException("Cancelled orders cannot be paid.");

        if (PaymentStatus == PaymentStatus.Paid)
            throw new DomainException("Order is already paid.");

        PaymentStatus = PaymentStatus.Paid;
        PaidOn = paidOn;
    }

    public void MarkAsCompleted()
    {
        if (Status != MealOrderStatus.Pending)
            throw new DomainException("Only pending orders can be completed.");

        Status = MealOrderStatus.Completed;
    }

    public void Cancel()
    {
        if (PaymentStatus == PaymentStatus.Paid)
            throw new DomainException("Paid orders cannot be cancelled.");

        if (Status == MealOrderStatus.Completed)
            throw new DomainException("Completed orders cannot be cancelled.");

        Status = MealOrderStatus.Cancelled;
    }

    public void SoftDelete()
    {
        if (IsDeleted)
            throw new DomainException("Order is already deleted.");

        if (Status == MealOrderStatus.Completed)
            throw new DomainException("Completed orders cannot be deleted.");

        if (Status == MealOrderStatus.Cancelled)
            throw new DomainException("Cancelled orders cannot be deleted.");

        IsDeleted = true;
    }

    public void UpdateMenuDate(DateTime newMenuDate)
    {
        if (IsDeleted)
            throw new DomainException("Cannot update menu date on a deleted order.");

        if (Status != MealOrderStatus.Pending)
            throw new DomainException("Only pending orders can have their menu date updated.");

        MenuDate = newMenuDate;
    }
}
