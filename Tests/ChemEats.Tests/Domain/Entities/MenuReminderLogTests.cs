using Domain.Entities;
using Domain.Infrastructure.Exceptions;

namespace ChemEats.Tests.Domain.Entities;

public class MenuReminderLogTests
{
    [Fact]
    public void Create_WhenInputIsValid_ShouldSetProperties()
    {
        Guid menuId = Guid.NewGuid();
        DateTime sentAt = new(2026, 10, 26, 10, 0, 0);

        MenuReminderLog log = MenuReminderLog.Create("user1", menuId, sentAt);

        Assert.NotEqual(Guid.Empty, log.Id);
        Assert.Equal("user1", log.UserId);
        Assert.Equal(menuId, log.MenuId);
        Assert.Equal(sentAt, log.SentAt);
    }

    [Fact]
    public void Create_WhenUserIdIsMissing_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => MenuReminderLog.Create(" ", Guid.NewGuid(), DateTime.Now));
    }

    [Fact]
    public void Create_WhenMenuIdIsEmpty_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => MenuReminderLog.Create("user1", Guid.Empty, DateTime.Now));
    }
}
