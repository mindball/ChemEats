using Domain.Entities;
using Domain.Repositories.MealOrders;

namespace Services.Repositories.MealOrders;

public class MealOrderRepository :  IMealOrderRepository
{
    public Task<MealOrder?> GetByIdAsync(MealOrderId Id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task AddAsync(MealOrder mealOrder, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}