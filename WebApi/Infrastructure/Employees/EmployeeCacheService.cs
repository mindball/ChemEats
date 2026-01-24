using Domain.Infrastructure.Identity;
using Domain.Repositories.Employees;
using Microsoft.Extensions.Caching.Memory;

namespace WebApi.Infrastructure.Employees;

public class EmployeeCacheService : IEmployeeCacheService
{
    private const string CacheKey = "EmployeeCache";
    private readonly IMemoryCache _cache;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<EmployeeCacheService> _logger;

    public EmployeeCacheService(
        IMemoryCache cache,
        IUserRepository userRepository,
        ILogger<EmployeeCacheService> logger)
    {
        _cache = cache;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing employee cache from database");
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

            List<ApplicationUser> users = await _userRepository.GetAllUsersAsync();

            sw.Stop();

            _cache.Set(CacheKey, users, TimeSpan.FromHours(12));

            _logger.LogInformation(
                "Employee cache initialized successfully with {UserCount} users in {ElapsedMs} ms (TTL: 12 hours)",
                users.Count,
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to initialize employee cache: {ErrorMessage}",
                ex.Message);
            throw;
        }
    }

    public async Task<ApplicationUser?> GetByAbbreviationAsync(string abbreviation)
    {
        try
        {
            if (!_cache.TryGetValue(CacheKey, out List<ApplicationUser>? users))
            {
                _logger.LogWarning(
                    "Employee cache miss when looking up {Abbreviation}. Reinitializing cache.",
                    abbreviation);

                await InitializeAsync();
                users = _cache.Get<List<ApplicationUser>>(CacheKey);
            }

            ApplicationUser? user = users?.FirstOrDefault(u =>
                u.Abbreviation.Equals(abbreviation, StringComparison.OrdinalIgnoreCase));

            if (user != null)
            {
                _logger.LogDebug(
                    "Employee {Abbreviation} found in cache: {UserName} (Id: {UserId})",
                    abbreviation,
                    user.UserName,
                    user.Id);
            }
            else
            {
                _logger.LogWarning(
                    "Employee {Abbreviation} not found in cache",
                    abbreviation);
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving employee {Abbreviation} from cache: {ErrorMessage}",
                abbreviation,
                ex.Message);
            return null;
        }
    }

    public IReadOnlyCollection<ApplicationUser> GetAll()
    {
        try
        {
            if (!_cache.TryGetValue(CacheKey, out List<ApplicationUser>? users))
            {
                _logger.LogWarning("Employee cache miss when getting all employees. Cache not initialized.");
                return [];
            }

            _logger.LogDebug("Retrieved {UserCount} employees from cache", users.Count);
            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving all employees from cache: {ErrorMessage}",
                ex.Message);
            return [];
        }
    }

    public async Task AddOrUpdateAsync(ApplicationUser employee)
    {
        try
        {
            if (!_cache.TryGetValue(CacheKey, out List<ApplicationUser>? users))
            {
                _logger.LogWarning(
                    "Cache miss when adding/updating employee {UserName}. Initializing empty list.",
                    employee.UserName);
                users = [];
            }

            ApplicationUser? existing = users.FirstOrDefault(u => u.Id == employee.Id);

            if (existing != null)
            {
                users.Remove(existing);
                _logger.LogInformation(
                    "Updating employee in cache: {UserName} ({Abbreviation}, Id: {UserId})",
                    employee.UserName,
                    employee.Abbreviation,
                    employee.Id);
            }
            else
            {
                _logger.LogInformation(
                    "Adding new employee to cache: {UserName} ({Abbreviation}, Id: {UserId})",
                    employee.UserName,
                    employee.Abbreviation,
                    employee.Id);
            }

            users.Add(employee);
            _cache.Set(CacheKey, users, TimeSpan.FromHours(12));

            _logger.LogInformation(
                "Employee cache updated successfully. Total employees in cache: {TotalCount}",
                users.Count);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error adding/updating employee {UserName} in cache: {ErrorMessage}",
                employee.UserName,
                ex.Message);
        }
    }
}