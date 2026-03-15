using Domain.Infrastructure.Identity;
using Domain.Repositories.Employees;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using WebApi.Infrastructure.Employees;

namespace ChemEats.Tests.WebApi.Infrastructure.Employees;

public class EmployeeCacheServiceTests
{
    [Fact]
    public async Task InitializeAsync_ShouldLoadUsersIntoCache()
    {
        List<ApplicationUser> users =
        [
            CreateUser("1", "MM", "Main Manager"),
            CreateUser("2", "DM", "Deputy Manager")
        ];

        Mock<IUserRepository> userRepositoryMock = new();
        userRepositoryMock.Setup(repository => repository.GetAllUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        using MemoryCache memoryCache = new(new MemoryCacheOptions());
        Mock<ILogger<EmployeeCacheService>> loggerMock = new();

        EmployeeCacheService service = new(memoryCache, userRepositoryMock.Object, loggerMock.Object);

        await service.InitializeAsync();

        IReadOnlyCollection<ApplicationUser> cachedUsers = service.GetAll();
        Assert.Equal(2, cachedUsers.Count);
    }

    [Fact]
    public async Task GetByAbbreviationAsync_WhenCacheMiss_ShouldInitializeAndReturnUser()
    {
        List<ApplicationUser> users =
        [
            CreateUser("1", "MM", "Main Manager")
        ];

        Mock<IUserRepository> userRepositoryMock = new();
        userRepositoryMock.Setup(repository => repository.GetAllUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        using MemoryCache memoryCache = new(new MemoryCacheOptions());
        Mock<ILogger<EmployeeCacheService>> loggerMock = new();

        EmployeeCacheService service = new(memoryCache, userRepositoryMock.Object, loggerMock.Object);

        ApplicationUser? result = await service.GetByAbbreviationAsync("mm");

        Assert.NotNull(result);
        Assert.Equal("1", result.Id);
        userRepositoryMock.Verify(repository => repository.GetAllUsersAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddOrUpdateAsync_WhenEmployeeExists_ShouldReplaceEmployee()
    {
        List<ApplicationUser> users =
        [
            CreateUser("1", "MM", "Old Name")
        ];

        Mock<IUserRepository> userRepositoryMock = new();
        using MemoryCache memoryCache = new(new MemoryCacheOptions());
        memoryCache.Set("EmployeeCache", users, TimeSpan.FromHours(12));

        Mock<ILogger<EmployeeCacheService>> loggerMock = new();
        EmployeeCacheService service = new(memoryCache, userRepositoryMock.Object, loggerMock.Object);

        ApplicationUser updatedUser = CreateUser("1", "MM", "New Name");

        await service.AddOrUpdateAsync(updatedUser);

        ApplicationUser? result = await service.GetByAbbreviationAsync("MM");
        Assert.NotNull(result);
        Assert.Equal("New Name", result.FullName);
    }

    [Fact]
    public async Task InitializeAsync_WhenRepositoryFails_ShouldRethrow()
    {
        Mock<IUserRepository> userRepositoryMock = new();
        userRepositoryMock.Setup(repository => repository.GetAllUsersAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        using MemoryCache memoryCache = new(new MemoryCacheOptions());
        Mock<ILogger<EmployeeCacheService>> loggerMock = new();

        EmployeeCacheService service = new(memoryCache, userRepositoryMock.Object, loggerMock.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.InitializeAsync());
    }

    private static ApplicationUser CreateUser(string id, string abbreviation, string fullName)
    {
        ApplicationUser user = new()
        {
            Id = id,
            UserName = abbreviation
        };

        user.SetProfile(fullName, abbreviation);
        return user;
    }
}
