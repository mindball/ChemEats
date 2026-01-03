using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Domain.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    private readonly List<MealOrder> _orders = [];

    public string Abbreviation { get; private set; } = null!;
    public string FullName { get; private set; } = null!;

    public IReadOnlyCollection<MealOrder> Orders => _orders.AsReadOnly();


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
}