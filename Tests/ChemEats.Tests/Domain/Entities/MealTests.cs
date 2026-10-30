using Domain.Entities;
using Domain.Infrastructure.Exceptions;

namespace ChemEats.Tests.Domain.Entities;

public class MealTests
{
    [Fact]
    public void Create_WhenNameIsLongerThan100Characters_ShouldKeepTheWholeName()
    {
        string name = "Малка млечно -чеснова Пица Бевърли Хилс/моцарела, пилешко филе, топено сирене, пресни домати, пармезан/";

        Meal meal = Meal.Create(Guid.NewGuid(), name, new Price(6.29m));

        Assert.Equal(name, meal.Name);
    }

    [Fact]
    public void Create_WhenNameIsExactlyMaxLength_ShouldCreateMeal()
    {
        string name = new('a', Meal.NameMaxLength);

        Meal meal = Meal.Create(Guid.NewGuid(), name, new Price(1m));

        Assert.Equal(Meal.NameMaxLength, meal.Name.Length);
    }

    [Fact]
    public void Create_WhenNameIsLongerThanMaxLength_ShouldThrowDomainException()
    {
        string name = new('a', Meal.NameMaxLength + 1);

        DomainException exception = Assert.Throws<DomainException>(() => Meal.Create(Guid.NewGuid(), name, new Price(1m)));

        Assert.Contains(Meal.NameMaxLength.ToString(), exception.Message);
    }

    [Fact]
    public void Create_WhenNameHasSurroundingSpaces_ShouldMeasureTheTrimmedName()
    {
        string name = $"  {new string('a', Meal.NameMaxLength)}  ";

        Meal meal = Meal.Create(Guid.NewGuid(), name, new Price(1m));

        Assert.Equal(Meal.NameMaxLength, meal.Name.Length);
    }
}
