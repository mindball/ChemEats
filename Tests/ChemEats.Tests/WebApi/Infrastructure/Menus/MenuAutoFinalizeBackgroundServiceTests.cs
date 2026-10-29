using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using WebApi.Infrastructure.Menus;

namespace ChemEats.Tests.WebApi.Infrastructure.Menus;

public class MenuAutoFinalizeBackgroundServiceTests
{
    [Fact]
    public async Task ExecuteAsync_WhenDisabled_ShouldNotFinalizeMenus()
    {
        Mock<IMenuAutoFinalizeService> finalizeServiceMock = new();
        MenuAutoFinalizeBackgroundService backgroundService = CreateBackgroundService(finalizeServiceMock.Object, enabled: false);

        await backgroundService.StartAsync(CancellationToken.None);
        await backgroundService.ExecuteTask!;
        await backgroundService.StopAsync(CancellationToken.None);

        finalizeServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_WhenEnabled_ShouldFinalizeImmediatelyOnStart()
    {
        TaskCompletionSource called = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Mock<IMenuAutoFinalizeService> finalizeServiceMock = new();
        finalizeServiceMock.Setup(service => service.FinalizeExpiredMenusAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback(() => called.TrySetResult())
            .ReturnsAsync(2);

        MenuAutoFinalizeBackgroundService backgroundService = CreateBackgroundService(finalizeServiceMock.Object, enabled: true);

        await backgroundService.StartAsync(CancellationToken.None);
        await called.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await backgroundService.StopAsync(CancellationToken.None);

        finalizeServiceMock.Verify(
            service => service.FinalizeExpiredMenusAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRunThrows_ShouldKeepRunning()
    {
        TaskCompletionSource called = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Mock<IMenuAutoFinalizeService> finalizeServiceMock = new();
        finalizeServiceMock.Setup(service => service.FinalizeExpiredMenusAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback(() => called.TrySetResult())
            .ThrowsAsync(new InvalidOperationException("database down"));

        MenuAutoFinalizeBackgroundService backgroundService = CreateBackgroundService(finalizeServiceMock.Object, enabled: true);

        await backgroundService.StartAsync(CancellationToken.None);
        await called.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(backgroundService.ExecuteTask!.IsFaulted);
        await backgroundService.StopAsync(CancellationToken.None);
    }

    private static MenuAutoFinalizeBackgroundService CreateBackgroundService(IMenuAutoFinalizeService finalizeService, bool enabled)
    {
        ServiceCollection services = new();
        services.AddScoped(_ => finalizeService);
        ServiceProvider provider = services.BuildServiceProvider();

        MenuAutoFinalizeOptions options = new()
        {
            Enabled = enabled,
            CheckIntervalMinutes = 60
        };

        return new MenuAutoFinalizeBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(options),
            NullLogger<MenuAutoFinalizeBackgroundService>.Instance);
    }
}
