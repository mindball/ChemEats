using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Domain.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    private readonly List<MealOrder> _orders = new();
    
    public string Abbreviation { get; set; }
    public IReadOnlyCollection<MealOrder> Orders => _orders.AsReadOnly();

    public string FullName { get; set; }
    
    public void AddOrder(MealOrder order)
    {
        if (order is null) throw new ArgumentNullException(nameof(order));
        _orders.Add(order);
    }
}