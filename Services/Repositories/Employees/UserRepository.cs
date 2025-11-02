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
        cancellationToken.ThrowIfCancellationRequested();
        var stringId = id.ToString();

        // Read-only by default: No tracking, preserve identity resolution for included collections
        return await _userManager.Users
            .AsNoTrackingWithIdentityResolution()
            .Include(u => u.Orders)
            .FirstOrDefaultAsync(u => u.Id == stringId, cancellationToken);
    }

    public async Task<List<ApplicationUser>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Read-only listing for UI - no tracking, keep identity resolution for included graphs
        return await _userManager.Users
            .AsNoTrackingWithIdentityResolution()
            .Include(u => u.Orders)
            .ToListAsync(cancellationToken);
    }

    public async Task<ApplicationUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        cancellationToken.ThrowIfCancellationRequested();
        var normalized = _userManager.NormalizeEmail(email);

        return await _userManager.Users
            .AsNoTrackingWithIdentityResolution()
            .Include(u => u.Orders)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, cancellationToken);
    }

    public async Task<ApplicationUser?> FindByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        cancellationToken.ThrowIfCancellationRequested();

        return await _userManager.Users
            .AsNoTrackingWithIdentityResolution()
            .Include(u => u.Orders)
            .FirstOrDefaultAsync(u => u.FullName == name, cancellationToken);
    }

    public async Task<ApplicationUser?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return null;

        cancellationToken.ThrowIfCancellationRequested();
        var normalized = _userManager.NormalizeName(userName);

        return await _userManager.Users
            .AsNoTrackingWithIdentityResolution()
            .Include(u => u.Orders)
            .FirstOrDefaultAsync(u => u.NormalizedUserName == normalized, cancellationToken);
    }

    // Helper: tracked variant for update scenarios. Add this to the repository so callers that intend to edit explicitly request a tracked entity.
    public async Task<ApplicationUser?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stringId = id.ToString();

        // Use DbContext directly to get a tracked entity (ChangeTracker will track it)
        return await _dbContext.Set<ApplicationUser>()
            .Include(u => u.Orders)
            .FirstOrDefaultAsync(u => u.Id == stringId, cancellationToken);
    }

    public Task<bool> RoleExistsAsync(string roleName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return Task.FromResult(false);

        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<bool>(cancellationToken);

        // RoleManager does not accept CancellationToken; forward call and return its task.
        return _roleManager.RoleExistsAsync(roleName);
    }

    public async Task<IdentityResult> CreateAsync(string roleName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            throw new ArgumentException("Role name cannot be null or empty.", nameof(roleName));

        if (await _roleManager.RoleExistsAsync(roleName))
        {
            _logger.LogInformation("Role {Role} already exists.", roleName);
            return IdentityResult.Success;
        }

        var result = await _roleManager.CreateAsync(new IdentityRole(roleName));

        if (result.Succeeded)
            _logger.LogInformation("Role {Role} created successfully.", roleName);
        else
            _logger.LogError("Failed to create role {Role}: {Errors}", roleName, string.Join(", ", result.Errors.Select(e => e.Description)));

        return result;
    }

    // Task<IdentityResult> IUserRepository.AddAsync(ApplicationUser user, string? password, string? role,
    //     CancellationToken cancellationToken)
    // {
    //     throw new NotImplementedException();
    // }

    public async Task<IdentityResult> AddToRoleAsync(ApplicationUser user, string role, CancellationToken cancellationToken = default)
    {
        if (user is null)
            throw new ArgumentNullException(nameof(user));
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role cannot be null or empty.", nameof(role));

        if (!await _roleManager.RoleExistsAsync(role))
        {
            var result = await _roleManager.CreateAsync(new IdentityRole(role));
            if (!result.Succeeded)
            {
                string errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to create role {Role}: {Errors}", role, errors);
                return result;
            }
        }

        var addResult = await _userManager.AddToRoleAsync(user, role);
        if (addResult.Succeeded)
            _logger.LogInformation("User {User} added to role {Role}.", user.UserName, role);
        else
            _logger.LogError("Failed to add user {User} to role {Role}: {Errors}", user.UserName, role, string.Join(", ", addResult.Errors.Select(e => e.Description)));

        return addResult;
    }

    public async Task<IdentityResult> AddAsync(ApplicationUser user, string? password, string? role,
        CancellationToken cancellationToken)
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
            return createResult;
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                var roleCreateResult = await _roleManager.CreateAsync(new IdentityRole(role));
                if (!roleCreateResult.Succeeded)
                    return roleCreateResult;
            }

            IdentityResult addToRoleResult = await _userManager.AddToRoleAsync(user, role);
            if (!addToRoleResult.Succeeded)
            {
                string addErrors = string.Join(", ", addToRoleResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to add user {User} to role {Role}: {Errors}", user.UserName, role, addErrors);
                return addToRoleResult;
            }
        }

        _logger.LogInformation("User {User} created successfully with role {Role}", user.UserName, role ?? "(none)");
        return IdentityResult.Success;
    }


    public async Task<bool> RoleExistsAsync(string roleName)
    {
        return await _roleManager.RoleExistsAsync(roleName);
    }
}