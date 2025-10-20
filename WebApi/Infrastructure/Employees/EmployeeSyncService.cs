using Domain;
using Domain.Infrastructure.Identity;
using Domain.Repositories.Employees;
using Microsoft.AspNetCore.Identity;
using Shared.DTOs.Employees;

namespace WebApi.Infrastructure.Employees;

public class EmployeeSyncService : IEmployeeSyncService
{
    private readonly AppDbContext _db;
    private readonly IUserRepository _userRepository;
    private readonly IEmployeeCacheService _employeeCache;
    private readonly IEmployeeExternalService _externalService;
    private readonly ILogger<EmployeeSyncService> _logger;
    // private readonly UserManager<ApplicationUser> _userManager;

    public EmployeeSyncService(
        AppDbContext db,
        // UserManager<ApplicationUser> userManager,
        IUserRepository userRepository,
        IEmployeeExternalService externalService,
        IEmployeeCacheService employeeCache,
        ILogger<EmployeeSyncService> logger
    )
    {
        _db = db;
        _userRepository = userRepository;
        _externalService = externalService;
        _employeeCache = employeeCache;
        _logger = logger;
    }

    public async Task SyncEmployeesAsync()
    {
        _logger.LogInformation("Starting employee sync from external service...");

        List<UserDto> employeesFromApi = await _externalService.GetAllEmployeesAsync();
        if (employeesFromApi.Count == 0)
        {
            _logger.LogWarning("No employees received from external API.");
            return;
        }

        foreach (UserDto dto in employeesFromApi)
        {
            ApplicationUser? existingUser = await _userRepository.FindByEmailAsync(dto.Code);

            if (existingUser == null)
            {
                // var newUser = new ApplicationUser();
                // await _db.Users.AddAsync(newUser);
                // _logger.LogInformation("New user added: {Name} ({Code})", dto.Name, dto.Code);
                // await _db.SaveChangesAsync();

                ApplicationUser user = new()
                {
                    Abbreviation = dto.Code.ToLowerInvariant(),
                    UserName = dto.Code.ToLowerInvariant(),
                    Email = $"{dto.Code.ToLowerInvariant()}@company.local",
                    EmailConfirmed = true
                };

                IdentityResult result = await _userRepository.AddAsync(user, "Employee@123");
                if (result.Succeeded)
                {
                    await _userRepository.AddToRoleAsync(user, "Employee");
                    _logger.LogInformation("Created ApplicationUser for employee {Code}", dto.Code);
                }
                else
                {
                    _logger.LogWarning("Failed to create user for {Code}: {Errors}",
                        dto.Code, string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                await _employeeCache.AddOrUpdateAsync(user);
            }
            //TODO make update functionality
            // existingUser.Update(dto.Name, dto.Code);
            // await _employeeCache.AddOrUpdateAsync(existingUser);
            // _db.Employees.Update(existingUser);
        }

        _logger.LogInformation("Employee synchronization and cache refresh complete.");
    }
}