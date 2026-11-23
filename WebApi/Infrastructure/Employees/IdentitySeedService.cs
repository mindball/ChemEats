using System.Security.Claims;
using Domain.Infrastructure.Identity;
using Domain.Repositories.Employees;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using WebApi.Infrastructure.Identity;

namespace WebApi.Infrastructure.Employees;

public class IdentitySeedService : IIdentitySeedService
{
    public static readonly string[] DefaultRoles = ["Admin", "Employee", "Manager", "Supervisor"];

    private readonly IUserRepository _userRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly BootstrapAdminOptions _bootstrapAdmin;
    private readonly ILogger<IdentitySeedService> _logger;

    public IdentitySeedService(
        IUserRepository userRepository,
        UserManager<ApplicationUser> userManager,
        IOptions<BootstrapAdminOptions> bootstrapAdmin,
        ILogger<IdentitySeedService> logger)
    {
        _userRepository = userRepository;
        _userManager = userManager;
        _bootstrapAdmin = bootstrapAdmin.Value;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await EnsureRolesAsync(cancellationToken);
        await EnsureBootstrapAdminAsync(cancellationToken);
    }

    private async Task EnsureRolesAsync(CancellationToken cancellationToken)
    {
        foreach (string role in DefaultRoles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await _userRepository.RoleExistsAsync(role, cancellationToken))
                continue;

            IdentityResult result = await _userRepository.CreateAsync(role, cancellationToken);
            if (result.Succeeded)
                _logger.LogInformation("Role {Role} created", role);
            else
                _logger.LogError("Failed to create role {Role}: {Errors}", role,
                    string.Join(", ", result.Errors.Select(error => error.Description)));
        }
    }

    // Creates the first admin on a fresh database, where nobody could log in to add employees otherwise.
    private async Task EnsureBootstrapAdminAsync(CancellationToken cancellationToken)
    {
        string abbreviation = ApplicationUser.NormalizeAbbreviation(_bootstrapAdmin.Abbreviation);
        if (abbreviation.Length == 0)
            return;

        if (await _userRepository.FindByUserNameAsync(abbreviation, cancellationToken) is not null)
            return;

        if (string.IsNullOrWhiteSpace(_bootstrapAdmin.Password))
        {
            _logger.LogWarning("Bootstrap admin {Abbreviation} not created: BootstrapAdmin:Password is not configured", abbreviation);
            return;
        }

        string fullName = string.IsNullOrWhiteSpace(_bootstrapAdmin.FullName) ? abbreviation : _bootstrapAdmin.FullName;
        ApplicationUser admin = ApplicationUser.CreateEmployee(abbreviation, fullName, _bootstrapAdmin.Email);

        IdentityResult result = await _userRepository.AddAsync(admin, _bootstrapAdmin.Password, "Employee", cancellationToken);
        if (result.Succeeded)
            result = await _userRepository.AddToRoleAsync(admin, "Admin", cancellationToken);
        if (result.Succeeded)
            result = await _userManager.AddClaimAsync(admin,
                new Claim(PasswordChangeRequirement.ClaimType, PasswordChangeRequirement.ClaimValue));

        if (result.Succeeded)
            _logger.LogInformation("Bootstrap admin {Abbreviation} created", abbreviation);
        else
            _logger.LogError("Failed to create bootstrap admin {Abbreviation}: {Errors}", abbreviation,
                string.Join(", ", result.Errors.Select(error => error.Description)));
    }
}
