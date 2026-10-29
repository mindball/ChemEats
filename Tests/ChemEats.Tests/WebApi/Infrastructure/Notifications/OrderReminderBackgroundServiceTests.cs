using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using WebApi.Infrastructure.Notifications;

namespace ChemEats.Tests.WebApi.Infrastructure.Notifications;

public class OrderReminderBackgroundServiceTests
{
    [Fact]
    public async Task ExecuteAsync_WhenDisabled_ShouldNotRunReminders()
    {
        Mock<IOrderReminderService> reminderServiceMock = new();
        OrderReminderBackgroundService backgroundService = CreateBackgroundService(reminderServiceMock.Object, enabled: false);

        await backgroundService.StartAsync(CancellationToken.None);
        await backgroundService.ExecuteTask!;
        await backgroundService.StopAsync(CancellationToken.None);

        reminderServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_WhenEnabled_ShouldRunRemindersImmediately()
    {
        TaskCompletionSource called = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Mock<IOrderReminderService> reminderServiceMock = new();
        reminderServiceMock.Setup(service => service.SendDueRemindersAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback(() => called.TrySetResult())
            .ReturnsAsync(0);

        OrderReminderBackgroundService backgroundService = CreateBackgroundService(reminderServiceMock.Object, enabled: true);

        await backgroundService.StartAsync(CancellationToken.None);
        await called.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await backgroundService.StopAsync(CancellationToken.None);

        reminderServiceMock.Verify(service => service.SendDueRemindersAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenReminderRunThrows_ShouldKeepRunning()
    {
        TaskCompletionSource called = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Mock<IOrderReminderService> reminderServiceMock = new();
        reminderServiceMock.Setup(service => service.SendDueRemindersAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback(() => called.TrySetResult())
            .ThrowsAsync(new InvalidOperationException("SMTP down"));

        OrderReminderBackgroundService backgroundService = CreateBackgroundService(reminderServiceMock.Object, enabled: true);

        await backgroundService.StartAsync(CancellationToken.None);
        await called.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(backgroundService.ExecuteTask!.IsFaulted);
        await backgroundService.StopAsync(CancellationToken.None);
    }

    private static OrderReminderBackgroundService CreateBackgroundService(IOrderReminderService reminderService, bool enabled)
    {
        ServiceCollection services = new();
        services.AddScoped(_ => reminderService);
        ServiceProvider provider = services.BuildServiceProvider();

        OrderReminderOptions options = new()
        {
            Enabled = enabled,
            CheckIntervalMinutes = 60
        };

        return new OrderReminderBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(options),
            NullLogger<OrderReminderBackgroundService>.Instance);
    }
}
