using Domain.Entities;
using Domain.Infrastructure.Exceptions;

namespace ChemEats.Tests.Domain.Entities;

public class OtherObligationTests
{
    [Fact]
    public void Create_WhenInputIsValid_ShouldCreateUnpaidObligationWithTrimmedDescriptionAndDateOnly()
    {
        DateTime createdAt = new(2026, 11, 3, 9, 15, 0);

        OtherObligation obligation = OtherObligation.Create(
            "user-1",
            "  Printer toner  ",
            12.50m,
            new DateTime(2026, 11, 2, 17, 45, 0),
            createdAt,
            "admin-1");

        Assert.NotEqual(Guid.Empty, obligation.Id);
        Assert.Equal("user-1", obligation.UserId);
        Assert.Equal("Printer toner", obligation.Description);
        Assert.Equal(12.50m, obligation.Amount);
        Assert.Equal(new DateTime(2026, 11, 2), obligation.Date);
        Assert.Equal(createdAt, obligation.CreatedAt);
        Assert.Equal("admin-1", obligation.CreatedByUserId);
        Assert.Equal(PaymentStatus.Unpaid, obligation.PaymentStatus);
        Assert.Null(obligation.PaidOn);
        Assert.False(obligation.IsDeleted);
    }

    [Theory]
    [InlineData("", "Toner", 1, "admin-1")]
    [InlineData("user-1", "", 1, "admin-1")]
    [InlineData("user-1", "   ", 1, "admin-1")]
    [InlineData("user-1", "Toner", 0, "admin-1")]
    [InlineData("user-1", "Toner", -5, "admin-1")]
    [InlineData("user-1", "Toner", 1, "")]
    public void Create_WhenInputIsInvalid_ShouldThrowDomainException(string userId, string description, int amount, string createdBy)
    {
        Assert.Throws<DomainException>(() =>
            OtherObligation.Create(userId, description, amount, DateTime.Today, DateTime.Now, createdBy));
    }

    [Fact]
    public void Create_WhenAmountHasMoreThanTwoDecimals_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() =>
            OtherObligation.Create("user-1", "Toner", 1.005m, DateTime.Today, DateTime.Now, "admin-1"));
    }

    [Fact]
    public void Create_WhenDescriptionIsTooLong_ShouldThrowDomainException()
    {
        string description = new('x', OtherObligation.DescriptionMaxLength + 1);

        Assert.Throws<DomainException>(() =>
            OtherObligation.Create("user-1", description, 1m, DateTime.Today, DateTime.Now, "admin-1"));
    }

    [Fact]
    public void Update_WhenUnpaid_ShouldChangeDescriptionAmountAndDate()
    {
        OtherObligation obligation = CreateObligation();

        obligation.Update(" Keyboard ", 30m, new DateTime(2026, 10, 15, 8, 0, 0));

        Assert.Equal("Keyboard", obligation.Description);
        Assert.Equal(30m, obligation.Amount);
        Assert.Equal(new DateTime(2026, 10, 15), obligation.Date);
    }

    [Fact]
    public void Update_WhenPaid_ShouldThrowDomainException()
    {
        OtherObligation obligation = CreateObligation();
        obligation.MarkAsPaid(DateTime.Now, "admin-1");

        Assert.Throws<DomainException>(() => obligation.Update("Keyboard", 30m, DateTime.Today));
    }

    [Fact]
    public void Update_WhenDeleted_ShouldThrowDomainException()
    {
        OtherObligation obligation = CreateObligation();
        obligation.SoftDelete();

        Assert.Throws<DomainException>(() => obligation.Update("Keyboard", 30m, DateTime.Today));
    }

    [Fact]
    public void Update_WhenAmountIsNotPositive_ShouldThrowDomainException()
    {
        OtherObligation obligation = CreateObligation();

        Assert.Throws<DomainException>(() => obligation.Update("Keyboard", 0m, DateTime.Today));
    }

    [Fact]
    public void MarkAsPaid_WhenUnpaid_ShouldSetPaymentFields()
    {
        OtherObligation obligation = CreateObligation();
        DateTime paidOn = new(2026, 11, 3, 10, 0, 0);

        obligation.MarkAsPaid(paidOn, "admin-2");

        Assert.Equal(PaymentStatus.Paid, obligation.PaymentStatus);
        Assert.Equal(paidOn, obligation.PaidOn);
        Assert.Equal("admin-2", obligation.PaidByUserId);
    }

    [Fact]
    public void MarkAsPaid_WhenAlreadyPaid_ShouldThrowDomainException()
    {
        OtherObligation obligation = CreateObligation();
        obligation.MarkAsPaid(DateTime.Now, "admin-1");

        Assert.Throws<DomainException>(() => obligation.MarkAsPaid(DateTime.Now, "admin-1"));
    }

    [Fact]
    public void MarkAsPaid_WhenPaidByIsMissing_ShouldThrowDomainException()
    {
        OtherObligation obligation = CreateObligation();

        Assert.Throws<DomainException>(() => obligation.MarkAsPaid(DateTime.Now, " "));
    }

    [Fact]
    public void MarkAsPaid_WhenDeleted_ShouldThrowDomainException()
    {
        OtherObligation obligation = CreateObligation();
        obligation.SoftDelete();

        Assert.Throws<DomainException>(() => obligation.MarkAsPaid(DateTime.Now, "admin-1"));
    }

    [Fact]
    public void SoftDelete_WhenUnpaid_ShouldMarkAsDeleted()
    {
        OtherObligation obligation = CreateObligation();

        obligation.SoftDelete();

        Assert.True(obligation.IsDeleted);
    }

    [Fact]
    public void SoftDelete_WhenPaid_ShouldThrowDomainException()
    {
        OtherObligation obligation = CreateObligation();
        obligation.MarkAsPaid(DateTime.Now, "admin-1");

        Assert.Throws<DomainException>(() => obligation.SoftDelete());
    }

    [Fact]
    public void SoftDelete_WhenAlreadyDeleted_ShouldThrowDomainException()
    {
        OtherObligation obligation = CreateObligation();
        obligation.SoftDelete();

        Assert.Throws<DomainException>(() => obligation.SoftDelete());
    }

    private static OtherObligation CreateObligation() =>
        OtherObligation.Create("user-1", "Printer toner", 12.5m, DateTime.Today, DateTime.Now, "admin-1");
}
