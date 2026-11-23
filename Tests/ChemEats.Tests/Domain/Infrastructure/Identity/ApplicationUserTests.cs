using ChemEats.Tests.TestInfrastructure;
using Domain.Infrastructure.Exceptions;
using Domain.Infrastructure.Identity;

namespace ChemEats.Tests.Domain.Infrastructure.Identity;

public class ApplicationUserTests
{
    [Fact]
    public void CreateEmployee_WhenInputIsValid_ShouldNormalizeAbbreviationAndUseItAsUserName()
    {
        ApplicationUser user = ApplicationUser.CreateEmployee(" dm ", " Dimitar M ", " dm@example.com ");

        Assert.Equal("DM", user.Abbreviation);
        Assert.Equal("DM", user.UserName);
        Assert.Equal("Dimitar M", user.FullName);
        Assert.Equal("dm@example.com", user.Email);
    }

    [Fact]
    public void CreateEmployee_WhenEmailIsEmpty_ShouldLeaveEmailNull()
    {
        ApplicationUser user = ApplicationUser.CreateEmployee("DM", "Dimitar", "  ");

        Assert.Null(user.Email);
    }

    [Theory]
    [InlineData("", "Name", null)]
    [InlineData("ABCD", "Name", null)]
    [InlineData("A-B", "Name", null)]
    [InlineData("DM", "", null)]
    [InlineData("DM", "Name", "bad email")]
    public void CreateEmployee_WhenInputIsInvalid_ShouldThrow(string abbreviation, string fullName, string? email)
    {
        Assert.Throws<DomainException>(() => ApplicationUser.CreateEmployee(abbreviation, fullName, email));
    }

    [Fact]
    public void UpdateNotificationSettings_WhenEmailIsValid_ShouldStoreTrimmedEmailAndFlag()
    {
        ApplicationUser user = TestDataFactory.CreateUser("u1", "MM", "Mincho");

        user.UpdateNotificationSettings("  personal@example.com ", true);

        Assert.Equal("personal@example.com", user.NotificationEmail);
        Assert.True(user.OrderRemindersEnabled);
        Assert.Equal("mm@cpachem.com", user.Email);
    }

    [Fact]
    public void UpdateNotificationSettings_WhenRemindersEnabledWithoutEmail_ShouldThrow()
    {
        ApplicationUser user = TestDataFactory.CreateUser("u1", "MM", "Mincho");

        Assert.Throws<DomainException>(() => user.UpdateNotificationSettings("   ", true));
        Assert.False(user.OrderRemindersEnabled);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("Name <name@example.com>")]
    [InlineData("name@")]
    public void UpdateNotificationSettings_WhenEmailIsInvalid_ShouldThrow(string email)
    {
        ApplicationUser user = TestDataFactory.CreateUser("u1", "MM", "Mincho");

        Assert.Throws<DomainException>(() => user.UpdateNotificationSettings(email, false));
        Assert.Null(user.NotificationEmail);
    }

    [Fact]
    public void UpdateNotificationSettings_WhenEmailIsTooLong_ShouldThrow()
    {
        ApplicationUser user = TestDataFactory.CreateUser("u1", "MM", "Mincho");
        string email = new string('a', ApplicationUser.NotificationEmailMaxLength) + "@example.com";

        Assert.Throws<DomainException>(() => user.UpdateNotificationSettings(email, true));
    }

    [Fact]
    public void UpdateNotificationSettings_WhenDisablingReminders_ShouldKeepEmail()
    {
        ApplicationUser user = TestDataFactory.CreateUserWithReminders("u1", "MM", "Mincho", "personal@example.com");

        user.UpdateNotificationSettings("personal@example.com", false);

        Assert.False(user.OrderRemindersEnabled);
        Assert.Equal("personal@example.com", user.NotificationEmail);
    }

    [Fact]
    public void UpdateNotificationSettings_WhenEmailClearedAndDisabled_ShouldClearEmail()
    {
        ApplicationUser user = TestDataFactory.CreateUserWithReminders("u1", "MM", "Mincho", "personal@example.com");

        user.UpdateNotificationSettings(null, false);

        Assert.Null(user.NotificationEmail);
        Assert.False(user.OrderRemindersEnabled);
    }
}
