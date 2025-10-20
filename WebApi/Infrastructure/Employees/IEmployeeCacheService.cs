using Domain.Entities;
using Domain.Infrastructure.Identity;

namespace WebApi.Infrastructure.Employees;

public interface IEmployeeCacheService
{
    Task InitializeAsync();
    Task<ApplicationUser?> GetByAbbreviationAsync(string abbreviation);
    IReadOnlyCollection<ApplicationUser> GetAll();
    Task AddOrUpdateAsync(ApplicationUser employee);
}