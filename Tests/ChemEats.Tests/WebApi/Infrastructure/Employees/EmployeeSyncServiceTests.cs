using Domain.Infrastructure.Identity;
using Domain.Repositories.Employees;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.DTOs.Employees;
using WebApi.Infrastructure.Employees;

namespace ChemEats.Tests.WebApi.Infrastructure.Employees;

public class EmployeeSyncServiceTests
{
    [Fact]
    public async Task SyncEmployeesAsync_WhenExternalReturnsEmptyCollection_ShouldStopAfterRoleInitialization()
    {
        Mock<IUserRepository> userRepository = CreateUserRepositoryWithExistingRoles();
        Mock<IEmployeeExternalService> externalService = new();
        externalService.Setup(service => service.GetAllEmployeesAsync())
            .ReturnsAsync([]);

        Mock<IEmployeeCacheService> cacheService = new();
        Mock<ILogger<EmployeeSyncService>> logger = new();

        EmployeeSyncService service = new(userRepository.Object, externalService.Object, cacheService.Object, logger.Object);

        await service.SyncEmployeesAsync();

        userRepository.Verify(repository => repository.AddAsync(It.IsAny<ApplicationUser>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        userRepository.Verify(repository => repository.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        cacheService.Verify(repository => repository.AddOrUpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task SyncEmployeesAsync_WhenNewAdminEmployeeIsReceived_ShouldCreateUserAssignAdminRoleAndCache()
    {
        UserDto adminDto = new() { Code = "MM", Name = "Main Manager" };

        Mock<IUserRepository> userRepository = CreateUserRepositoryWithExistingRoles();
        userRepository.Setup(repository => repository.FindByUserNameAsync("MM", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);
        userRepository.Setup(repository => repository.AddAsync(It.IsAny<ApplicationUser>(), "MM", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResult.Success);
        userRepository.Setup(repository => repository.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResult.Success);

        Mock<IEmployeeExternalService> externalService = new();
        externalService.Setup(service => service.GetAllEmployeesAsync())
            .ReturnsAsync([adminDto]);

        Mock<IEmployeeCacheService> cacheService = new();
        Mock<ILogger<EmployeeSyncService>> logger = new();

        EmployeeSyncService employeeSyncService = new(userRepository.Object, externalService.Object, cacheService.Object, logger.Object);

        await employeeSyncService.SyncEmployeesAsync();

        userRepository.Verify(repository => repository.AddAsync(It.Is<ApplicationUser>(user => user.UserName == "MM" && user.Email == "mm@cpachem.com"), "MM", It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        userRepository.Verify(repository => repository.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Admin", It.IsAny<CancellationToken>()), Times.Once);
        cacheService.Verify(service => service.AddOrUpdateAsync(It.Is<ApplicationUser>(user => user.Abbreviation == "MM" && user.FullName == "Main Manager")), Times.Once);
    }

    [Fact]
    public async Task SyncEmployeesAsync_WhenEmployeeAlreadyExists_ShouldSkipCreateAndRoleAssignment()
    {
        ApplicationUser existingUser = new() { Id = Guid.NewGuid().ToString(), UserName = "AB" };

        Mock<IUserRepository> userRepository = CreateUserRepositoryWithExistingRoles();
        userRepository.Setup(repository => repository.FindByUserNameAsync("AB", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        Mock<IEmployeeExternalService> externalService = new();
        externalService.Setup(service => service.GetAllEmployeesAsync())
            .ReturnsAsync([new UserDto { Code = "AB", Name = "Already Exists" }]);

        Mock<IEmployeeCacheService> cacheService = new();
        Mock<ILogger<EmployeeSyncService>> logger = new();

        EmployeeSyncService employeeSyncService = new(userRepository.Object, externalService.Object, cacheService.Object, logger.Object);

        await employeeSyncService.SyncEmployeesAsync();

        userRepository.Verify(repository => repository.AddAsync(It.IsAny<ApplicationUser>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        userRepository.Verify(repository => repository.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        cacheService.Verify(service => service.AddOrUpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task SyncEmployeesAsync_WhenUserCreationFails_ShouldNotAssignRoleOrUpdateCache()
    {
        UserDto employee = new() { Code = "E1", Name = "Employee One" };

        Mock<IUserRepository> userRepository = CreateUserRepositoryWithExistingRoles();
        userRepository.Setup(repository => repository.FindByUserNameAsync("E1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);
        userRepository.Setup(repository => repository.AddAsync(It.IsAny<ApplicationUser>(), "E1", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Create failed" }));

        Mock<IEmployeeExternalService> externalService = new();
        externalService.Setup(service => service.GetAllEmployeesAsync())
            .ReturnsAsync([employee]);

        Mock<IEmployeeCacheService> cacheService = new();
        Mock<ILogger<EmployeeSyncService>> logger = new();

        EmployeeSyncService employeeSyncService = new(userRepository.Object, externalService.Object, cacheService.Object, logger.Object);

        await employeeSyncService.SyncEmployeesAsync();

        userRepository.Verify(repository => repository.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        cacheService.Verify(service => service.AddOrUpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task SyncEmployeesAsync_WhenExternalServiceThrows_ShouldRethrow()
    {
        Mock<IUserRepository> userRepository = CreateUserRepositoryWithExistingRoles();

        Mock<IEmployeeExternalService> externalService = new();
        externalService.Setup(service => service.GetAllEmployeesAsync())
            .ThrowsAsync(new InvalidOperationException("External API unavailable"));

        Mock<IEmployeeCacheService> cacheService = new();
        Mock<ILogger<EmployeeSyncService>> logger = new();

        EmployeeSyncService service = new(userRepository.Object, externalService.Object, cacheService.Object, logger.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SyncEmployeesAsync());
    }

    private static Mock<IUserRepository> CreateUserRepositoryWithExistingRoles()
    {
        Mock<IUserRepository> userRepository = new();

        userRepository.Setup(repository => repository.RoleExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        return userRepository;
    }
}
