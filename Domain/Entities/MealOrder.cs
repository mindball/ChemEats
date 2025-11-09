using Domain.Infrastructure.Identity;

namespace Domain.Entities;

public enum MealOrderStatus
{
    Pending,
    Completed,
    Cancelled
}

public class MealOrder
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; }
    public ApplicationUser? User { get; private set; }

    public Guid MealId { get; private set; }
    public Meal? Meal { get; private set; }

    public DateTime Date { get; private set; }
    public MealOrderStatus Status { get; private set; }

    private MealOrder() { } 

    private MealOrder(Guid id, string userId, Guid mealId, DateTime date)
    {
        Id = id;
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        MealId = mealId;
        Date = date;
        Status = MealOrderStatus.Pending;
    }

    private MealOrder(Guid id, ApplicationUser user, Guid mealId, DateTime date)
        : this(id, user?.Id ?? throw new ArgumentNullException(nameof(user)), mealId, date)
    {
        User = user;
    }

    public static MealOrder Create(Guid id, string userId, Guid mealId, DateTime date)
    {
        return new MealOrder(id, userId, mealId, date);
    }

    public static MealOrder Create(Guid id, ApplicationUser user, Guid mealId, DateTime date)
    {
        return new MealOrder(id, user, mealId, date);
    }

    public void MarkAsCompleted()
    {
        if (Status != MealOrderStatus.Pending)
            throw new InvalidOperationException("Only pending orders can be completed.");

        Status = MealOrderStatus.Completed;
    }
}