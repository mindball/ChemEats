using Domain;
using Domain.Infrastructure.Identity;
using Domain.Repositories.Employees;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Services.Repositories.Employees;

public class UserRepository : IUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        AppDbContext dbContext,
        ILogger<UserRepository> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        string stringId = id.ToString();

        return await _userManager.Users
            .Include(u => u.Orders)
            .FirstOrDefaultAsync(u => u.Id == stringId, cancellationToken);
    }

    public Task<List<ApplicationUser>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<ApplicationUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<ApplicationUser?> FindByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<ApplicationUser?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> RoleExistsAsync(string roleName, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IdentityResult> CreateAsync(string roleName, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    Task<IdentityResult> IUserRepository.AddAsync(ApplicationUser user, string? password, string? role,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IdentityResult> AddToRoleAsync(ApplicationUser user, string role, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }


    public async Task AddAsync(ApplicationUser user, string? password = null, string? role = null,
        CancellationToken cancellationToken = default)
    {
        if (user is null)
            throw new ArgumentNullException(nameof(user));

        IdentityResult createResult = password is null
            ? await _userManager.CreateAsync(user)
            : await _userManager.CreateAsync(user, password);

        if (!createResult.Succeeded)
        {
            string errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            _logger.LogError("Failed to create user {User}: {Errors}", user.UserName, errors);
            throw new InvalidOperationException($"Failed to create user: {errors}");
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                _logger.LogInformation("Role {Role} not found. Creating...", role);
                IdentityResult roleResult = await _roleManager.CreateAsync(new IdentityRole(role));
                if (!roleResult.Succeeded)
                {
                    string roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create role '{role}': {roleErrors}");
                }
            }

            IdentityResult addToRoleResult = await _userManager.AddToRoleAsync(user, role);
            if (!addToRoleResult.Succeeded)
            {
                string addErrors = string.Join(", ", addToRoleResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException(
                    $"Failed to add user '{user.UserName}' to role '{role}': {addErrors}");
            }

            _logger.LogInformation("User {User} created and assigned to role {Role}", user.UserName, role);
        }
        else
        {
            _logger.LogInformation("User {User} created without role", user.UserName);
        }
    }

    
    public async Task<bool> RoleExistsAsync(string roleName)
    {
        return await _roleManager.RoleExistsAsync(roleName);
    }
}