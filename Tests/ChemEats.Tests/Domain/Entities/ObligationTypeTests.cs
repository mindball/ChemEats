using Domain.Entities;
using Domain.Infrastructure.Exceptions;

namespace ChemEats.Tests.Domain.Entities;

public class ObligationTypeTests
{
    [Fact]
    public void Create_WhenNameIsValid_ShouldTrimName()
    {
        ObligationType type = ObligationType.Create("  Printer toner ");

        Assert.NotEqual(Guid.Empty, type.Id);
        Assert.Equal("Printer toner", type.Name);
        Assert.False(type.IsDeleted);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenNameIsEmpty_ShouldThrowDomainException(string name)
    {
        Assert.Throws<DomainException>(() => ObligationType.Create(name));
    }

    [Fact]
    public void Create_WhenNameIsTooLong_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => ObligationType.Create(new string('x', ObligationType.NameMaxLength + 1)));
    }

    [Fact]
    public void Rename_WhenActive_ShouldChangeName()
    {
        ObligationType type = ObligationType.Create("Toner");

        type.Rename(" Keyboard ");

        Assert.Equal("Keyboard", type.Name);
    }

    [Fact]
    public void Rename_WhenDeleted_ShouldThrowDomainException()
    {
        ObligationType type = ObligationType.Create("Toner");
        type.SoftDelete();

        Assert.Throws<DomainException>(() => type.Rename("Keyboard"));
    }

    [Fact]
    public void SoftDelete_WhenAlreadyDeleted_ShouldThrowDomainException()
    {
        ObligationType type = ObligationType.Create("Toner");
        type.SoftDelete();

        Assert.True(type.IsDeleted);
        Assert.Throws<DomainException>(() => type.SoftDelete());
    }
}
