using Domain.Repositories.MealOrders;

namespace WebApi.Infrastructure.Menus;

// Does the same as the manual "Finalize" button, but for menus whose ordering window has already closed,
// so their orders become payable even if nobody finalized them by hand.
public sealed class MenuAutoFinalizeService : IMenuAutoFinalizeService
{
    private readonly IMealOrderRepository _orderRepository;
    private readonly ILogger<MenuAutoFinalizeService> _logger;

    public MenuAutoFinalizeService(
        IMealOrderRepository orderRepository,
        ILogger<MenuAutoFinalizeService> logger)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> FinalizeExpiredMenusAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Guid> menuIds = await _orderRepository.GetExpiredMenuIdsWithPendingOrdersAsync(now, cancellationToken);

        if (menuIds.Count == 0)
            return 0;

        int totalCompleted = 0;

        foreach (Guid menuId in menuIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                (int completedCount, decimal totalAmount) =
                    await _orderRepository.MarkPendingOrdersAsCompletedForMenuAsync(menuId, cancellationToken);

                totalCompleted += completedCount;

                _logger.LogInformation(
                    "Expired menu {MenuId} auto-finalized: {CompletedCount} orders marked as completed, total amount {TotalAmount}",
                    menuId,
                    completedCount,
                    totalAmount);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // One failing menu must not block the others; it is retried on the next run.
                _logger.LogError(ex, "Auto-finalize failed for menu {MenuId}: {ErrorMessage}", menuId, ex.Message);
            }
        }

        return totalCompleted;
    }
}
