using Domain.Entities;
using Domain.Infrastructure.Exceptions;

namespace ChemEats.Tests.Domain.Entities;

public class MealOrderTests
{
    [Fact]
    public void Create_WhenUserIdIsMissing_ShouldThrow()
    {
        DomainException exception = Assert.Throws<DomainException>(() => MealOrder.Create("", Guid.NewGuid(), DateTime.Today.AddDays(1), 10m));

        Assert.Equal("User ID is required to create an order.", exception.Message);
    }

    [Fact]
    public void Create_WhenMealIdIsEmpty_ShouldThrow()
    {
        DomainException exception = Assert.Throws<DomainException>(() => MealOrder.Create("MM", Guid.Empty, DateTime.Today.AddDays(1), 10m));

        Assert.Equal("Meal ID is required to create an order.", exception.Message);
    }

    [Fact]
    public void Create_WhenPriceSnapshotIsNegative_ShouldThrow()
    {
        DomainException exception = Assert.Throws<DomainException>(() => MealOrder.Create("MM", Guid.NewGuid(), DateTime.Today.AddDays(1), -1m));

        Assert.Equal("Price snapshot cannot be negative.", exception.Message);
    }

    [Fact]
    public void ApplyPortion_WhenPortionExceedsPrice_ShouldThrow()
    {
        MealOrder order = MealOrder.Create("MM", Guid.NewGuid(), DateTime.Today.AddDays(1), 10m);

        DomainException exception = Assert.Throws<DomainException>(() => order.ApplyPortion(10.01m));

        Assert.Equal("Portion cannot exceed price.", exception.Message);
    }

    [Fact]
    public void GetNetAmount_WhenPortionApplied_ShouldReturnExpectedNetValue()
    {
        MealOrder order = MealOrder.Create("MM", Guid.NewGuid(), DateTime.Today.AddDays(1), 20m);
        order.ApplyPortion(5m);

        decimal result = order.GetNetAmount();

        Assert.Equal(15m, result);
    }

    [Fact]
    public void MarkAsPaid_WhenOrderAlreadyPaid_ShouldThrow()
    {
        MealOrder order = MealOrder.Create("MM", Guid.NewGuid(), DateTime.Today.AddDays(1), 20m);
        order.MarkAsPaid(DateTime.UtcNow);

        DomainException exception = Assert.Throws<DomainException>(() => order.MarkAsPaid(DateTime.UtcNow));

        Assert.Equal("Order is already paid.", exception.Message);
    }

    [Fact]
    public void Cancel_WhenOrderIsPaid_ShouldThrow()
    {
        MealOrder order = MealOrder.Create("MM", Guid.NewGuid(), DateTime.Today.AddDays(1), 20m);
        order.MarkAsPaid(DateTime.UtcNow);

        DomainException exception = Assert.Throws<DomainException>(() => order.Cancel());

        Assert.Equal("Paid orders cannot be cancelled.", exception.Message);
    }

    [Fact]
    public void SoftDelete_WhenOrderIsPending_ShouldMarkAsDeleted()
    {
        MealOrder order = MealOrder.Create("MM", Guid.NewGuid(), DateTime.Today.AddDays(1), 20m);

        order.SoftDelete();

        Assert.True(order.IsDeleted);
    }

    [Fact]
    public void UpdateMenuDate_WhenOrderIsDeleted_ShouldThrow()
    {
        MealOrder order = MealOrder.Create("MM", Guid.NewGuid(), DateTime.Today.AddDays(1), 20m);
        order.SoftDelete();

        DomainException exception = Assert.Throws<DomainException>(() => order.UpdateMenuDate(DateTime.Today.AddDays(2)));

        Assert.Equal("Cannot update menu date on a deleted order.", exception.Message);
    }
}
