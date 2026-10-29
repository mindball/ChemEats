using System.Net.Mail;
using Domain.Entities;
using Domain.Models.Notifications;
using Domain.Repositories.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using WebApi.Infrastructure.Notifications;

namespace ChemEats.Tests.WebApi.Infrastructure.Notifications;

public class OrderReminderServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 27, 10, 0, 0);
    private static readonly DateTime MenuDate = new(2026, 10, 29);

    [Fact]
    public async Task SendDueRemindersAsync_WhenNoCandidates_ShouldNotSendEmails()
    {
        Mock<IOrderReminderRepository> repositoryMock = new();
        repositoryMock.Setup(repository => repository.GetDueRemindersAsync(Now, TimeSpan.FromMinutes(120), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        Mock<IEmailService> emailServiceMock = new();

        OrderReminderService service = CreateService(repositoryMock.Object, emailServiceMock.Object);

        int sent = await service.SendDueRemindersAsync(Now);

        Assert.Equal(0, sent);
        emailServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendDueRemindersAsync_WhenUserMissesTwoMenusSameDate_ShouldSendOneEmailAndLogBothMenus()
    {
        Guid menuA = Guid.NewGuid();
        Guid menuB = Guid.NewGuid();
        Mock<IOrderReminderRepository> repositoryMock = new();
        repositoryMock.Setup(repository => repository.GetDueRemindersAsync(Now, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreateCandidate("u1", "Ivan <Ivanov>", "ivan@example.com", menuA, "Supplier A"),
                CreateCandidate("u1", "Ivan <Ivanov>", "ivan@example.com", menuB, "Supplier B")
            ]);

        List<MenuReminderLog> savedLogs = [];
        repositoryMock.Setup(repository => repository.AddReminderLogsAsync(It.IsAny<IEnumerable<MenuReminderLog>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<MenuReminderLog>, CancellationToken>((logs, _) => savedLogs.AddRange(logs))
            .Returns(Task.CompletedTask);

        string? capturedBody = null;
        Mock<IEmailService> emailServiceMock = new();
        emailServiceMock.Setup(email => email.SendAsync("ivan@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, _, body, _) => capturedBody = body)
            .Returns(Task.CompletedTask);

        OrderReminderService service = CreateService(repositoryMock.Object, emailServiceMock.Object, "http://chemeats.local");

        int sent = await service.SendDueRemindersAsync(Now);

        Assert.Equal(1, sent);
        emailServiceMock.Verify(email => email.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(new[] { menuA, menuB }.Order(), savedLogs.Select(log => log.MenuId).Order());
        Assert.All(savedLogs, log => Assert.Equal(Now, log.SentAt));
        Assert.NotNull(capturedBody);
        Assert.Contains("Supplier A", capturedBody);
        Assert.Contains("Supplier B", capturedBody);
        Assert.Contains("Ivan &lt;Ivanov&gt;", capturedBody);
        Assert.Contains("http://chemeats.local", capturedBody);
    }

    [Fact]
    public async Task SendDueRemindersAsync_WhenEmailFailsForOneUser_ShouldNotLogItAndContinueWithOthers()
    {
        Guid menuId = Guid.NewGuid();
        Mock<IOrderReminderRepository> repositoryMock = new();
        repositoryMock.Setup(repository => repository.GetDueRemindersAsync(Now, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreateCandidate("u1", "User One", "one@example.com", menuId, "Supplier"),
                CreateCandidate("u2", "User Two", "two@example.com", menuId, "Supplier")
            ]);

        List<MenuReminderLog> savedLogs = [];
        repositoryMock.Setup(repository => repository.AddReminderLogsAsync(It.IsAny<IEnumerable<MenuReminderLog>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<MenuReminderLog>, CancellationToken>((logs, _) => savedLogs.AddRange(logs))
            .Returns(Task.CompletedTask);

        Mock<IEmailService> emailServiceMock = new();
        emailServiceMock.Setup(email => email.SendAsync("one@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SmtpException("mailbox unavailable"));
        emailServiceMock.Setup(email => email.SendAsync("two@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        OrderReminderService service = CreateService(repositoryMock.Object, emailServiceMock.Object);

        int sent = await service.SendDueRemindersAsync(Now);

        Assert.Equal(1, sent);
        MenuReminderLog log = Assert.Single(savedLogs);
        Assert.Equal("u2", log.UserId);
    }

    [Fact]
    public async Task SendDueRemindersAsync_WhenLogSaveFails_ShouldStillCountSentEmail()
    {
        Mock<IOrderReminderRepository> repositoryMock = new();
        repositoryMock.Setup(repository => repository.GetDueRemindersAsync(Now, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateCandidate("u1", "User One", "one@example.com", Guid.NewGuid(), "Supplier")]);
        repositoryMock.Setup(repository => repository.AddReminderLogsAsync(It.IsAny<IEnumerable<MenuReminderLog>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));
        Mock<IEmailService> emailServiceMock = new();

        OrderReminderService service = CreateService(repositoryMock.Object, emailServiceMock.Object);

        int sent = await service.SendDueRemindersAsync(Now);

        Assert.Equal(1, sent);
    }

    [Fact]
    public async Task SendDueRemindersAsync_ShouldPassConfiguredLeadTimeToRepository()
    {
        Mock<IOrderReminderRepository> repositoryMock = new();
        repositoryMock.Setup(repository => repository.GetDueRemindersAsync(It.IsAny<DateTime>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        OrderReminderService service = CreateService(repositoryMock.Object, Mock.Of<IEmailService>(), leadTimeMinutes: 45);

        await service.SendDueRemindersAsync(Now);

        repositoryMock.Verify(repository => repository.GetDueRemindersAsync(Now, TimeSpan.FromMinutes(45), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static OrderReminderCandidate CreateCandidate(string userId, string name, string email, Guid menuId, string supplierName) =>
        new(userId, name, email, menuId, MenuDate, new DateTime(2026, 10, 27, 11, 30, 0), supplierName);

    private static OrderReminderService CreateService(
        IOrderReminderRepository repository,
        IEmailService emailService,
        string? applicationUrl = null,
        int leadTimeMinutes = 120)
    {
        OrderReminderOptions options = new()
        {
            Enabled = true,
            LeadTimeMinutes = leadTimeMinutes,
            ApplicationUrl = applicationUrl
        };

        return new OrderReminderService(repository, emailService, Options.Create(options), NullLogger<OrderReminderService>.Instance);
    }
}
