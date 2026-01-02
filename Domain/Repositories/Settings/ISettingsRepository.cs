namespace Domain.Repositories.Settings;

public interface ISettingsRepository
{
    Task<decimal> GetCompanyPortionAsync(CancellationToken cancellationToken = default);
    Task SetCompanyPortionAsync(decimal portionAmount, CancellationToken cancellationToken = default);
}