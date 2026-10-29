using Microsoft.Extensions.Options;

namespace WebApi.Infrastructure.Notifications;

public sealed class OrderReminderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OrderReminderOptions _options;
    private readonly ILogger<OrderReminderBackgroundService> _logger;

    public OrderReminderBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<OrderReminderOptions> options,
        ILogger<OrderReminderBackgroundService> logger)
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
            _logger.LogInformation("Order reminders are disabled ({Section}:Enabled = false)", OrderReminderOptions.SectionName);
            return;
        }

        TimeSpan interval = TimeSpan.FromMinutes(Math.Max(1, _options.CheckIntervalMinutes));
        _logger.LogInformation("Order reminder job started: checking every {IntervalMinutes} min, lead time {LeadTimeMinutes} min",
            interval.TotalMinutes, _options.LeadTimeMinutes);

        using PeriodicTimer timer = new(interval);

        try
        {
            do
            {
                await RunOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Order reminder job stopped");
        }
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            IOrderReminderService reminderService = scope.ServiceProvider.GetRequiredService<IOrderReminderService>();

            int sentCount = await reminderService.SendDueRemindersAsync(DateTime.Now, stoppingToken);
            if (sentCount > 0)
                _logger.LogInformation("Order reminder run finished: {SentCount} reminders sent", sentCount);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Order reminder run failed");
        }
    }
}
