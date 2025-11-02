using Domain.Infrastructure.Identity;
using Domain.Repositories.Employees;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Shared.DTOs.Employees;

namespace WebApi.Infrastructure.Employees;

public class EmployeeCacheService : IEmployeeCacheService
{
    private const string CacheKey = "EmployeeCache";
    private readonly IMemoryCache _cache;
    private readonly IEmployeeExternalService _externalService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<EmployeeCacheService> _logger;

    private static readonly string[] DefaultRoles = ["Admin", "Employee", "Manager"];
    private static readonly string[] AdminEmployeeCodes = ["MM", "DM"];

    public EmployeeCacheService(
        IEmployeeExternalService externalService,
        ILogger<EmployeeCacheService> logger,
        IMemoryCache cache,
        IUserRepository userRepository)
    {
        _externalService = externalService;
        _logger = logger;
        _cache = cache;
        _userRepository = userRepository;
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("Initializing employee cache and syncing Identity users...");

        foreach (string role in DefaultRoles)
        {
            if (!await _userRepository.RoleExistsAsync(role))
            {
                await _userRepository.CreateAsync(role);
                _logger.LogInformation("Created missing role: {Role}", role);
            }
        }

        List<UserDto> employeesFromApi = await _externalService.GetAllEmployeesAsync();
        if (employeesFromApi.Count == 0)
        {
            _logger.LogWarning("No employees fetched from API.");
            return;
        }

        foreach (UserDto dto in employeesFromApi)
        {
            ApplicationUser? existing = await _userRepository.FindByUserNameAsync(dto.Code);

            if (existing == null)
            {
                ApplicationUser user = dto.Adapt<ApplicationUser>();
                user.UserName = dto.Code;
                user.Email = $"{dto.Code.ToLower()}@cpachem.com";
                user.EmailConfirmed = true;

                IdentityResult createResult = await _userRepository.AddAsync(user, dto.Code);

                if (createResult.Succeeded)
                {
                    string roleToAssign = AdminEmployeeCodes.Contains(dto.Code)
                        ? "Admin"
                        : "Employee";
                    await _userRepository.AddToRoleAsync(user, roleToAssign);

                    _logger.LogInformation("Created user {User} with role {Role}", user.UserName, roleToAssign);
                }
                else
                {
                    _logger.LogError("Failed to create user {User}: {Errors}",
                        dto.Code,
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
            }
        }

        List<ApplicationUser> allEmployees = await _userRepository.GetAllUsersAsync();
        _cache.Set(CacheKey, allEmployees, TimeSpan.FromHours(12));

        _logger.LogInformation("Employee cache initialized with {Count} records.", allEmployees.Count);
    }

    public async Task<ApplicationUser?> GetByAbbreviationAsync(string abbreviation)
    {
        _logger.LogInformation("Entered {Service}.{Method}", nameof(IEmployeeCacheService), nameof(GetByAbbreviationAsync));

        if (!_cache.TryGetValue(CacheKey, out List<ApplicationUser>? users))
        {
            await InitializeAsync();
            users = _cache.Get<List<ApplicationUser>>(CacheKey);
        }

        return users?.FirstOrDefault(e =>
            e.Abbreviation.Equals(abbreviation, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyCollection<ApplicationUser> GetAll()
    {
        _logger.LogInformation("Entered {Service}.{Method}", nameof(IEmployeeCacheService), nameof(GetAll));

        if (!_cache.TryGetValue(CacheKey, out List<ApplicationUser>? users))
            return [];

        return users ?? [];
    }

    public async Task AddOrUpdateAsync(ApplicationUser employee)
    {
        _logger.LogInformation("Entered {Service}.{Method}", nameof(IEmployeeCacheService), nameof(AddOrUpdateAsync));

        if (!_cache.TryGetValue(CacheKey, out List<ApplicationUser>? users))
            users = [];

        ApplicationUser? existing = users.FirstOrDefault(e => e.Id == employee.Id);
        if (existing != null)
            users.Remove(existing);

        users.Add(employee);
        _cache.Set(CacheKey, users, TimeSpan.FromHours(12));

        await Task.CompletedTask;
    }
}
