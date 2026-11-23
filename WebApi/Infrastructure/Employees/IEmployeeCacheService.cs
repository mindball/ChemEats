using Domain.Entities;
using Domain.Infrastructure.Identity;

namespace WebApi.Infrastructure.Employees;

[Obsolete("External HR employee integration is disabled: the isolated host has no access to the HR service. Employees are created manually by an admin. Kept for a possible future integration.")]
public interface IEmployeeCacheService
{
    Task InitializeAsync();
    Task<ApplicationUser?> GetByAbbreviationAsync(string abbreviation);
    IReadOnlyCollection<ApplicationUser> GetAll();
    Task AddOrUpdateAsync(ApplicationUser employee);
    Task RemoveByAbbreviationAsync(string abbreviation);
}