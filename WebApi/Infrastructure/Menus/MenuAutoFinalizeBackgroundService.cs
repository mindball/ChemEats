using Microsoft.Extensions.Options;

namespace WebApi.Infrastructure.Menus;

public sealed class MenuAutoFinalizeBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MenuAutoFinalizeOptions _options;
    private readonly ILogger<MenuAutoFinalizeBackgroundService> _logger;

    public MenuAutoFinalizeBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<MenuAutoFinalizeOptions> options,
        ILogger<MenuAutoFinalizeBackgroundService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _options = options.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Menu auto-finalize is disabled ({Section}:Enabled = false)", MenuAutoFinalizeOptions.SectionName);
            return;
        }

        TimeSpan interval = TimeSpan.FromMinutes(Math.Max(1, _options.CheckIntervalMinutes));
        _logger.LogInformation("Menu auto-finalize job started: checking every {IntervalMinutes} min", interval.TotalMinutes);

        using PeriodicTimer timer = new(interval);

        try
        {
            // The first run happens immediately, which also catches menus that expired while the service was down.
            do
            {
                await RunOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Menu auto-finalize job stopped");
        }
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            IMenuAutoFinalizeService finalizeService = scope.ServiceProvider.GetRequiredService<IMenuAutoFinalizeService>();

            int completedCount = await finalizeService.FinalizeExpiredMenusAsync(DateTime.Now, stoppingToken);
            if (completedCount > 0)
                _logger.LogInformation("Menu auto-finalize run finished: {CompletedCount} orders marked as completed", completedCount);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Menu auto-finalize run failed");
        }
    }
}
