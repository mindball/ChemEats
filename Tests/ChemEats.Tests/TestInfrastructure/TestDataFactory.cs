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

    public static ApplicationUser CreateUserWithReminders(string userId, string code, string fullName, string notificationEmail)
    {
        ApplicationUser user = CreateUser(userId, code, fullName);
        user.UpdateNotificationSettings(notificationEmail, true);
        return user;
    }

    public static MealOrder CreateOrder(string userId, Guid mealId, DateTime menuDate, decimal price)
    {
        return MealOrder.Create(userId, mealId, menuDate, price);
    }

    public static OtherObligation CreateObligation(
        string userId,
        decimal amount = 12.5m,
        DateTime? date = null,
        string description = "Printer toner",
        string createdByUserId = "admin-id")
    {
        return OtherObligation.Create(userId, description, amount, date ?? DateTime.Today, DateTime.Now, createdByUserId);
    }

    // An order whose menu was finalized; only these can be paid.
    public static MealOrder CreateCompletedOrder(string userId, Guid mealId, DateTime menuDate, decimal price)
    {
        MealOrder order = MealOrder.Create(userId, mealId, menuDate, price);
        order.MarkAsCompleted();
        return order;
    }
}
