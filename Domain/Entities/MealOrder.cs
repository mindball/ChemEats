using Domain.Common.Enums;
using Domain.Infrastructure.Identity;
using System.ComponentModel.DataAnnotations;

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
    public string UserId { get; private set; }
    public ApplicationUser? User { get; private set; }

    public Guid MealId { get; private set; }
    public Meal Meal { get; private set; } = null!;

    public DateTime MenuDate { get; private set; }     
    public DateTime OrderedAt { get; private set; }    


    public MealOrderStatus Status { get; private set; }

    public PaymentStatus PaymentStatus { get; private set; }
    public DateTime? PaidOn { get; private set; }

    public bool IsDeleted { get; private set; }

    // New: snapshot of the meal price at order time (immutable snapshot)
    public decimal PriceAmount { get; private set; }

    // New: portion application flags on this order
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

        PriceAmount = 0m;
        PortionApplied = false;
        PortionAmount = 0m;
    }

    public static MealOrder Create(
        string userId,
        Guid mealId,
        DateTime menuDate,
        decimal priceSnapshot)
    {
        if (priceSnapshot < 0)
            throw new ArgumentOutOfRangeException(nameof(priceSnapshot));

        return new MealOrder(Guid.NewGuid(), userId, mealId, menuDate, priceSnapshot);
    }

    public void SetPriceSnapshot(decimal priceAmount)
    {
        if (priceAmount < 0) throw new ArgumentOutOfRangeException(nameof(priceAmount));
        PriceAmount = priceAmount;
    }

    public void ApplyPortion(decimal portionAmount)
    {
        if (portionAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(portionAmount));

        if (portionAmount > PriceAmount)
            throw new InvalidOperationException("Portion cannot exceed price.");

        PortionApplied = portionAmount > 0m;
        PortionAmount = portionAmount;
    }

    public decimal GetNetAmount() =>
        Math.Max(0m, PriceAmount - (PortionApplied ? PortionAmount : 0m));

    public void MarkAsPaid(DateTime paidOn)
    {
        if (Status == MealOrderStatus.Cancelled)
            throw new InvalidOperationException("Cancelled orders cannot be paid.");

        if (PaymentStatus == PaymentStatus.Paid)
            throw new InvalidOperationException("Order already paid.");

        PaymentStatus = PaymentStatus.Paid;
        PaidOn = paidOn;
    }

    public void MarkAsCompleted()
    {
        if (Status != MealOrderStatus.Pending)
            throw new InvalidOperationException("Only pending orders can be completed.");

        Status = MealOrderStatus.Completed;
    }

    public void Cancel()
    {
        if (PaymentStatus == PaymentStatus.Paid)
            throw new InvalidOperationException("Paid orders cannot be cancelled");

        if (Status == MealOrderStatus.Completed)
            throw new InvalidOperationException("Completed orders cannot be cancelled.");

        Status = MealOrderStatus.Cancelled;
    }

    public void SoftDelete() => IsDeleted = true;
}

public static class PaymentTermsExtensions
{
    public static DateTime CalculateDueDate(this PaymentTerms terms, DateTime orderDate)
    {
        return orderDate.Date.AddDays((int)terms);
    }
}
