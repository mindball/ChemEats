namespace WebApp.Services.Settings;

public interface ISettingsDataService
{
    Task<decimal> GetCompanyPortionAsync();
    Task<bool> SetCompanyPortionAsync(decimal portionAmount);
}