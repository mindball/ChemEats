using Domain.Repositories.Settings;

namespace Services.Repositories.Settings;

public sealed class InMemorySettingsRepository : ISettingsRepository
{
    private static decimal _portionAmount = 3.00m; 

    public Task<decimal> GetCompanyPortionAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_portionAmount);

    public Task SetCompanyPortionAsync(decimal portionAmount, CancellationToken cancellationToken = default)
    {
        _portionAmount = portionAmount < 0 ? 0 : portionAmount;
        return Task.CompletedTask;
    }
}