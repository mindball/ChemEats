using Domain.Common.Enums;
using Domain.Entities;
using Domain.Infrastructure.Identity;

namespace ChemEats.Tests.TestInfrastructure;

internal static class TestDataFactory
{
    public static Supplier CreateSupplier(Guid? id = null, string name = "Supplier")
    {
        Supplier supplier = new(id ?? Guid.NewGuid(), name, $"VAT-{Guid.NewGuid():N}"[..12], PaymentTerms.Net10);
        return supplier;
    }

    public static Menu CreateMenu(Guid supplierId, string mealName = "Meal", decimal price = 10m, DateTime? date = null, DateTime? activeUntil = null)
    {
        DateTime menuDate = date ?? DateTime.Today.AddDays(3);
        DateTime activeUntilDate = activeUntil ?? DateTime.Today.AddDays(1).AddHours(12);

        Meal seedMeal = Meal.Create(Guid.NewGuid(), mealName, new Price(price));
        return Menu.Create(supplierId, menuDate, activeUntilDate, [seedMeal]);
    }

    public static ApplicationUser CreateUser(string userId, string code, string fullName)
    {
        ApplicationUser user = new()
        {
            Id = userId,
            UserName = code,
            NormalizedUserName = code.ToUpperInvariant(),
            Email = $"{code.ToLowerInvariant()}@cpachem.com",
            NormalizedEmail = $"{code.ToUpperInvariant()}@CPACHEM.COM",
            EmailConfirmed = true
        };

        user.SetProfile(fullName, code);
        return user;
    }

    public static MealOrder CreateOrder(string userId, Guid mealId, DateTime menuDate, decimal price)
    {
        return MealOrder.Create(userId, mealId, menuDate, price);
    }
}
