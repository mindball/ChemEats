using Domain.Infrastructure.Identity;
using Domain.Repositories.Employees;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using WebApi.Infrastructure.Employees;

namespace ChemEats.Tests.WebApi.Infrastructure.Employees;

public class IdentitySeedServiceTests
{
    [Fact]
    public async Task SeedAsync_WhenRolesAreMissing_ShouldCreateThem()
    {
        Mock<IUserRepository> userRepositoryMock = new();
        userRepositoryMock.Setup(repository => repository.RoleExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        userRepositoryMock.Setup(repository => repository.CreateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(IdentityResult.Success);

        IdentitySeedService service = CreateService(userRepositoryMock, new BootstrapAdminOptions());

        await service.SeedAsync();

        foreach (string role in IdentitySeedService.DefaultRoles)
            userRepositoryMock.Verify(repository => repository.CreateAsync(role, It.IsAny<CancellationToken>()), Times.Once);
        userRepositoryMock.Verify(repository => repository.AddAsync(
            It.IsAny<ApplicationUser>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SeedAsync_WhenBootstrapAdminConfiguredAndMissing_ShouldCreateAdmin()
    {
        Mock<IUserRepository> userRepositoryMock = new();
        userRepositoryMock.Setup(repository => repository.RoleExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        userRepositoryMock.Setup(repository => repository.FindByUserNameAsync("MM", It.IsAny<CancellationToken>())).ReturnsAsync((ApplicationUser?)null);
        userRepositoryMock.Setup(repository => repository.AddAsync(It.IsAny<ApplicationUser>(), "MM", "Employee", It.IsAny<CancellationToken>())).ReturnsAsync(IdentityResult.Success);
        userRepositoryMock.Setup(repository => repository.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Admin", It.IsAny<CancellationToken>())).ReturnsAsync(IdentityResult.Success);

        IdentitySeedService service = CreateService(userRepositoryMock, new BootstrapAdminOptions { Abbreviation = "mm", FullName = "Mincho" });

        await service.SeedAsync();

        userRepositoryMock.Verify(repository => repository.AddToRoleAsync(
            It.Is<ApplicationUser>(user => user.Abbreviation == "MM"), "Admin", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SeedAsync_WhenBootstrapAdminAlreadyExists_ShouldNotCreateUser()
    {
        Mock<IUserRepository> userRepositoryMock = new();
        userRepositoryMock.Setup(repository => repository.RoleExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        userRepositoryMock.Setup(repository => repository.FindByUserNameAsync("MM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationUser.CreateEmployee("MM", "Mincho", null));

        IdentitySeedService service = CreateService(userRepositoryMock, new BootstrapAdminOptions { Abbreviation = "MM" });

        await service.SeedAsync();

        userRepositoryMock.Verify(repository => repository.AddAsync(
            It.IsAny<ApplicationUser>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static IdentitySeedService CreateService(Mock<IUserRepository> userRepositoryMock, BootstrapAdminOptions options) =>
        new(userRepositoryMock.Object, Options.Create(options), NullLogger<IdentitySeedService>.Instance);
}
