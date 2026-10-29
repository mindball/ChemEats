namespace WebApi.Infrastructure.Menus;

public interface IMenuAutoFinalizeService
{
    // Completes the pending orders of every menu whose ActiveUntil has passed; returns the number of completed orders.
    Task<int> FinalizeExpiredMenusAsync(DateTime now, CancellationToken cancellationToken = default);
}
