using Domain.Entities;
using StronglyTypedIds;

namespace Domain.Entities;

public enum MealOrderStatus
{
    Pending, 
    Completed,
    Cancelled
}

[StronglyTypedId]
public partial struct MealOrderId { }

public class MealOrder : Entity<MealOrderId>
{
    private MealOrder() { }

    public MealOrder(MealOrderId id, EmployeeId employeeId, MealId mealId, DateOnly date)
    {
        Id = id;
        EmployeeId = employeeId;
        MealId = mealId;
        Date = date;
    }

    public EmployeeId EmployeeId { get; private set; }
    public Employee? Employee { get; private set; } 

    public MealId MealId { get; private set; }
    public Meal? Meal { get; private set; } 

    public DateOnly Date { get; private set; }
    public MealOrderStatus Status { get; set; }
}