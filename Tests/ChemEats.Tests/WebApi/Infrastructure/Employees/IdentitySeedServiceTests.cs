using System.Security.Claims;
using Domain.Infrastructure.Identity;
using Domain.Repositories.Employees;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using WebApi.Infrastructure.Employees;
using WebApi.Infrastructure.Identity;

namespace ChemEats.Tests.WebApi.Infrastructure.Employees;

public class IdentitySeedServiceTests
{
    [Fact]
    public async Task SeedAsync_WhenRolesAreMissing_ShouldCreateThem()
    {
        Mock<IUserRepository> userRepositoryMock = new();
        userRepositoryMock.Setup(repository => repository.RoleExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        userRepositoryMock.Setup(repository => repository.CreateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(IdentityResult.Success);

        IdentitySeedService service = CreateService(userRepositoryMock, CreateUserManagerMock(), new BootstrapAdminOptions());

        await service.SeedAsync();

        foreach (string role in IdentitySeedService.DefaultRoles)
            userRepositoryMock.Verify(repository => repository.CreateAsync(role, It.IsAny<CancellationToken>()), Times.Once);
        userRepositoryMock.Verify(repository => repository.AddAsync(
            It.IsAny<ApplicationUser>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SeedAsync_WhenBootstrapAdminConfiguredAndMissing_ShouldCreateAdminThatMustChangePassword()
    {
        Mock<IUserRepository> userRepositoryMock = new();
        userRepositoryMock.Setup(repository => repository.RoleExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        userRepositoryMock.Setup(repository => repository.FindByUserNameAsync("MM", It.IsAny<CancellationToken>())).ReturnsAsync((ApplicationUser?)null);
        userRepositoryMock.Setup(repository => repository.AddAsync(It.IsAny<ApplicationUser>(), "Temp1234x", "Employee", It.IsAny<CancellationToken>())).ReturnsAsync(IdentityResult.Success);
        userRepositoryMock.Setup(repository => repository.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Admin", It.IsAny<CancellationToken>())).ReturnsAsync(IdentityResult.Success);
        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.AddClaimAsync(It.IsAny<ApplicationUser>(), It.IsAny<Claim>())).ReturnsAsync(IdentityResult.Success);

        IdentitySeedService service = CreateService(
            userRepositoryMock,
            userManagerMock,
            new BootstrapAdminOptions { Abbreviation = "mm", FullName = "Mincho", Password = "Temp1234x" });

        await service.SeedAsync();

        userRepositoryMock.Verify(repository => repository.AddToRoleAsync(
            It.Is<ApplicationUser>(user => user.Abbreviation == "MM"), "Admin", It.IsAny<CancellationToken>()), Times.Once);
        userManagerMock.Verify(manager => manager.AddClaimAsync(
            It.IsAny<ApplicationUser>(),
            It.Is<Claim>(claim => claim.Type == PasswordChangeRequirement.ClaimType)), Times.Once);
    }

    [Fact]
    public async Task SeedAsync_WhenBootstrapPasswordIsMissing_ShouldNotCreateUser()
    {
        Mock<IUserRepository> userRepositoryMock = new();
        userRepositoryMock.Setup(repository => repository.RoleExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        userRepositoryMock.Setup(repository => repository.FindByUserNameAsync("MM", It.IsAny<CancellationToken>())).ReturnsAsync((ApplicationUser?)null);

        IdentitySeedService service = CreateService(userRepositoryMock, CreateUserManagerMock(), new BootstrapAdminOptions { Abbreviation = "MM" });

        await service.SeedAsync();

        userRepositoryMock.Verify(repository => repository.AddAsync(
            It.IsAny<ApplicationUser>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SeedAsync_WhenBootstrapAdminAlreadyExists_ShouldNotCreateUser()
    {
        Mock<IUserRepository> userRepositoryMock = new();
        userRepositoryMock.Setup(repository => repository.RoleExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        userRepositoryMock.Setup(repository => repository.FindByUserNameAsync("MM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationUser.CreateEmployee("MM", "Mincho", null));

        IdentitySeedService service = CreateService(
            userRepositoryMock,
            CreateUserManagerMock(),
            new BootstrapAdminOptions { Abbreviation = "MM", Password = "Temp1234x" });

        await service.SeedAsync();

        userRepositoryMock.Verify(repository => repository.AddAsync(
            It.IsAny<ApplicationUser>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static IdentitySeedService CreateService(
        Mock<IUserRepository> userRepositoryMock,
        Mock<UserManager<ApplicationUser>> userManagerMock,
        BootstrapAdminOptions options) =>
        new(userRepositoryMock.Object, userManagerMock.Object, Options.Create(options), NullLogger<IdentitySeedService>.Instance);

    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        Mock<IUserStore<ApplicationUser>> userStoreMock = new();
        return new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}
