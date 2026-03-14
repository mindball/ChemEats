using Domain.Entities;

namespace ChemEats.Tests.Domain.Entities;

public class PriceTests
{
    [Fact]
    public void Constructor_WhenAmountIsNegative_ShouldThrow()
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Price(-0.01m));

        Assert.Equal("amount", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1.25)]
    [InlineData(100.99)]
    public void Constructor_WhenAmountIsValid_ShouldSetAmount(decimal amount)
    {
        Price price = new(amount);

        Assert.Equal(amount, price.Amount);
    }

    [Fact]
    public void Add_ShouldReturnSum()
    {
        Price left = new(5.50m);
        Price right = new(3.25m);

        Price result = left.Add(right);

        Assert.Equal(8.75m, result.Amount);
    }

    [Fact]
    public void Subtract_WhenResultWouldBeNegative_ShouldClampToZero()
    {
        Price left = new(2.00m);
        Price right = new(3.00m);

        Price result = left.Subtract(right);

        Assert.Equal(0m, result.Amount);
    }

    [Fact]
    public void ComparisonOperators_ShouldBehaveCorrectly()
    {
        Price low = new(1m);
        Price high = new(2m);

        Assert.True(high > low);
        Assert.True(low < high);
        Assert.True(high >= low);
        Assert.True(low <= high);
        Assert.True(high != low);
        Assert.True(high == new Price(2m));
    }
}
