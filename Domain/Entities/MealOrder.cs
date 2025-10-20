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
    private MealOrder()
    {
    }

    public MealOrder(Guid id, ApplicationUser user, Guid mealId, DateOnly date)
    {
        Id = id;
        User = user;
        MealId = mealId;
        Date = date;
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; }
    public ApplicationUser? User { get; private set; }

    public Guid MealId { get; private set; }
    public Meal? Meal { get; private set; }

    public DateOnly Date { get; private set; }
    public MealOrderStatus Status { get; set; }
}