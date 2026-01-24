using Domain;
using Domain.Infrastructure.Identity;
using Domain.Repositories.Employees;
using Microsoft.AspNetCore.Identity;
using Shared.DTOs.Employees;

namespace WebApi.Infrastructure.Employees;

public class EmployeeSyncService : IEmployeeSyncService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmployeeExternalService _externalService;
    private readonly IEmployeeCacheService _employeeCache;
    private readonly ILogger<EmployeeSyncService> _logger;

    private static readonly string[] DefaultRoles = ["Admin", "Employee", "Manager"];
    private static readonly string[] AdminEmployeeCodes = ["MM", "DM"];

    public EmployeeSyncService(
        IUserRepository userRepository,
        IEmployeeExternalService externalService,
        IEmployeeCacheService employeeCache,
        ILogger<EmployeeSyncService> logger)
    {
        _userRepository = userRepository;
        _externalService = externalService;
        _employeeCache = employeeCache;
        _logger = logger;
    }

    public async Task SyncEmployeesAsync()
    {
        _logger.LogInformation("Starting employee synchronization process");
        System.Diagnostics.Stopwatch totalSw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Ensuring default roles exist: {Roles}", string.Join(", ", DefaultRoles));
            int rolesCreated = 0;

            foreach (string role in DefaultRoles)
            {
                if (!await _userRepository.RoleExistsAsync(role))
                {
                    await _userRepository.CreateAsync(role);
                    rolesCreated++;
                    _logger.LogInformation("Role {Role} created successfully", role);
                }
            }

            if (rolesCreated > 0)
            {
                _logger.LogInformation("Created {RolesCreated} new roles", rolesCreated);
            }
            else
            {
                _logger.LogInformation("All default roles already exist");
            }

            _logger.LogInformation("Fetching employees from external API");
            System.Diagnostics.Stopwatch fetchSw = System.Diagnostics.Stopwatch.StartNew();

            List<UserDto> employees = await _externalService.GetAllEmployeesAsync();

            fetchSw.Stop();

            if (employees.Count == 0)
            {
                _logger.LogWarning("No employees received from external API. Sync aborted.");
                return;
            }

            _logger.LogInformation(
                "Fetched {EmployeeCount} employees from external API in {ElapsedMs} ms",
                employees.Count,
                fetchSw.ElapsedMilliseconds);

            int created = 0;
            int skipped = 0;
            int failed = 0;

            foreach (UserDto dto in employees)
            {
                try
                {
                    ApplicationUser? existing = await _userRepository.FindByUserNameAsync(dto.Code);

                    if (existing == null)
                    {
                        _logger.LogInformation(
                            "Processing new employee: {Code} ({Name})",
                            dto.Code,
                            dto.Name);

                        ApplicationUser user = new()
                        {
                            UserName = dto.Code,
                            Email = $"{dto.Code.ToLower()}@cpachem.com",
                            EmailConfirmed = true
                        };

                        user.SetProfile(dto.Name, dto.Code);

                        IdentityResult result = await _userRepository.AddAsync(user, dto.Code);

                        if (!result.Succeeded)
                        {
                            failed++;
                            _logger.LogWarning(
                                "Failed to create user {Code} ({Name}): {Errors}",
                                dto.Code,
                                dto.Name,
                                string.Join(", ", result.Errors.Select(e => e.Description)));
                            continue;
                        }

                        string role = AdminEmployeeCodes.Contains(dto.Code) ? "Admin" : "Employee";

                        await _userRepository.AddToRoleAsync(user, role);
                        _logger.LogInformation(
                            "User {Code} assigned to role {Role}",
                            dto.Code,
                            role);

                        await _employeeCache.AddOrUpdateAsync(user);

                        created++;
                        _logger.LogInformation(
                            "User {Code} ({Name}) created successfully with role {Role}",
                            dto.Code,
                            dto.Name,
                            role);
                    }
                    else
                    {
                        skipped++;
                        _logger.LogDebug(
                            "User {Code} already exists, skipping (Id: {UserId})",
                            dto.Code,
                            existing.Id);
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogError(ex,
                        "Error processing employee {Code} ({Name}): {ErrorMessage}",
                        dto.Code,
                        dto.Name,
                        ex.Message);
                }
            }

            totalSw.Stop();

            _logger.LogInformation(
                "Employee synchronization completed in {ElapsedMs} ms - Created: {Created}, Skipped: {Skipped}, Failed: {Failed}, Total: {Total}",
                totalSw.ElapsedMilliseconds,
                created,
                skipped,
                failed,
                employees.Count);
        }
        catch (Exception ex)
        {
            totalSw.Stop();
            _logger.LogCritical(ex,
                "Employee synchronization failed critically after {ElapsedMs} ms: {ErrorMessage}",
                totalSw.ElapsedMilliseconds,
                ex.Message);
            throw;
        }
    }
}