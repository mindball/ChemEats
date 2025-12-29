using Domain.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Domain.Repositories.Employees;

public interface IUserRepository
{
    Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ApplicationUser>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<ApplicationUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> FindByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default);
    Task<bool> RoleExistsAsync(string roleName, CancellationToken cancellationToken = default);
    Task<IdentityResult> CreateAsync(string roleName, CancellationToken cancellationToken = default);
    Task<IdentityResult> AddAsync(ApplicationUser user, string? password = null, string? role = null, CancellationToken cancellationToken = default);
    Task<IdentityResult> AddToRoleAsync(ApplicationUser user, string role, CancellationToken cancellationToken = default);
    Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken = default);
}