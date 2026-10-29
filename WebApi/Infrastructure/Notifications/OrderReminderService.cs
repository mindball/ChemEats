using System.Globalization;
using System.Net;
using System.Text;
using Domain.Entities;
using Domain.Models.Notifications;
using Domain.Repositories.Notifications;
using Microsoft.Extensions.Options;

namespace WebApi.Infrastructure.Notifications;

public sealed class OrderReminderService : IOrderReminderService
{
    private readonly IOrderReminderRepository _repository;
    private readonly IEmailService _emailService;
    private readonly OrderReminderOptions _options;
    private readonly ILogger<OrderReminderService> _logger;

    public OrderReminderService(
        IOrderReminderRepository repository,
        IEmailService emailService,
        IOptions<OrderReminderOptions> options,
        ILogger<OrderReminderService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _options = options.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> SendDueRemindersAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        TimeSpan leadTime = TimeSpan.FromMinutes(Math.Max(0, _options.LeadTimeMinutes));
        List<OrderReminderCandidate> candidates = await _repository.GetDueRemindersAsync(now, leadTime, cancellationToken);

        if (candidates.Count == 0)
            return 0;

        int sentCount = 0;

        // Several menus (suppliers) can share a date; the user gets one email per date.
        foreach (IGrouping<(string UserId, DateTime MenuDate), OrderReminderCandidate> group in candidates
                     .GroupBy(candidate => (candidate.UserId, candidate.MenuDate.Date)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<OrderReminderCandidate> menus = group.OrderBy(candidate => candidate.ActiveUntil).ToList();
            OrderReminderCandidate first = menus[0];

            try
            {
                await _emailService.SendAsync(
                    first.NotificationEmail,
                    BuildSubject(group.Key.MenuDate),
                    BuildBody(first.EmployeeName, group.Key.MenuDate, menus, _options.ApplicationUrl),
                    cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // No log row is written, so the next run retries while the menu is still open.
                _logger.LogError(ex, "Failed to send order reminder to user {UserId} for {MenuDate:yyyy-MM-dd}",
                    group.Key.UserId, group.Key.MenuDate);
                continue;
            }

            try
            {
                await _repository.AddReminderLogsAsync(
                    menus.Select(candidate => MenuReminderLog.Create(candidate.UserId, candidate.MenuId, now)),
                    cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Order reminder sent to user {UserId} for {MenuDate:yyyy-MM-dd} but the reminder log could not be saved",
                    group.Key.UserId, group.Key.MenuDate);
            }

            sentCount++;
            _logger.LogInformation("Order reminder sent to user {UserId} for {MenuDate:yyyy-MM-dd} ({MenuCount} menus)",
                group.Key.UserId, group.Key.MenuDate, menus.Count);
        }

        return sentCount;
    }

    private static string BuildSubject(DateTime menuDate) =>
        $"ChemEats reminder: you have not ordered for {menuDate.ToString("dddd, dd.MM.yyyy", CultureInfo.InvariantCulture)}";

    private static string BuildBody(
        string employeeName,
        DateTime menuDate,
        IReadOnlyCollection<OrderReminderCandidate> menus,
        string? applicationUrl)
    {
        StringBuilder body = new();
        body.Append("<p>Hello ").Append(WebUtility.HtmlEncode(employeeName)).Append(",</p>");
        body.Append("<p>You have not ordered a meal for <strong>")
            .Append(menuDate.ToString("dddd, dd.MM.yyyy", CultureInfo.InvariantCulture))
            .Append("</strong> yet.</p>");

        body.Append("<ul>");
        foreach (OrderReminderCandidate menu in menus)
        {
            body.Append("<li>")
                .Append(WebUtility.HtmlEncode(menu.SupplierName))
                .Append(" &ndash; orders close at ")
                .Append(menu.ActiveUntil.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture))
                .Append("</li>");
        }
        body.Append("</ul>");

        if (!string.IsNullOrWhiteSpace(applicationUrl))
        {
            body.Append("<p><a href=\"")
                .Append(WebUtility.HtmlEncode(applicationUrl))
                .Append("\">Open ChemEats to place your order</a></p>");
        }

        body.Append("<p style=\"color:#777;font-size:12px\">You receive this email because order reminders are enabled ")
            .Append("in your ChemEats notification settings. You can turn them off there at any time.</p>");

        return body.ToString();
    }
}
