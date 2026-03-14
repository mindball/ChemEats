using ChemEats.Tests.TestInfrastructure;
using Domain;
using Domain.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Services.Repositories.Employees;

namespace ChemEats.Tests.Services.Repositories.Employees;

public sealed class UserRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        _context = TestDbContextFactory.Create();

        Mock<IUserStore<ApplicationUser>> userStoreMock = new();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);

        Mock<IRoleStore<IdentityRole>> roleStoreMock = new();
        _roleManagerMock = new Mock<RoleManager<IdentityRole>>(
            roleStoreMock.Object,
            null!,
            null!,
            null!,
            null!);

        Mock<ILogger<UserRepository>> loggerMock = new();

        _userManagerMock.SetupGet(x => x.Users).Returns(_context.Users);
        _userManagerMock.Setup(x => x.NormalizeEmail(It.IsAny<string>())).Returns<string>(email => email.ToUpperInvariant());
        _userManagerMock.Setup(x => x.NormalizeName(It.IsAny<string>())).Returns<string>(name => name.ToUpperInvariant());

        _repository = new UserRepository(_userManagerMock.Object, _roleManagerMock.Object, _context, loggerMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUser_WhenExists()
    {
        ApplicationUser user = TestDataFactory.CreateUser(Guid.NewGuid().ToString(), "MM", "Main Manager");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        ApplicationUser? result = await _repository.GetByIdAsync(Guid.Parse(user.Id));

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCancellationRequested_ShouldThrow()
    {
        using CancellationTokenSource cancellationTokenSource = new();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => _repository.GetByIdAsync(Guid.NewGuid(), cancellationTokenSource.Token));
    }

    [Fact]
    public async Task GetAllUsersAsync_ShouldReturnAllUsers()
    {
        await _context.Users.AddRangeAsync(
            TestDataFactory.CreateUser(Guid.NewGuid().ToString(), "A1", "User A"),
            TestDataFactory.CreateUser(Guid.NewGuid().ToString(), "B1", "User B"));
        await _context.SaveChangesAsync();

        List<ApplicationUser> users = await _repository.GetAllUsersAsync();

        Assert.Equal(2, users.Count);
    }

    [Fact]
    public async Task FindByEmailAsync_WhenEmailIsWhitespace_ShouldReturnNull()
    {
        ApplicationUser? result = await _repository.FindByEmailAsync(" ");

        Assert.Null(result);
    }

    [Fact]
    public async Task FindByEmailAsync_ShouldReturnUser_WhenEmailMatches()
    {
        ApplicationUser user = TestDataFactory.CreateUser(Guid.NewGuid().ToString(), "AA", "Alice A");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        ApplicationUser? result = await _repository.FindByEmailAsync(user.Email!);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
    }

    [Fact]
    public async Task FindByNameAsync_ShouldReturnUser_WhenNameMatches()
    {
        ApplicationUser user = TestDataFactory.CreateUser(Guid.NewGuid().ToString(), "CC", "Chris C");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        ApplicationUser? result = await _repository.FindByNameAsync("Chris C");

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
    }

    [Fact]
    public async Task FindByUserNameAsync_ShouldReturnUser_WhenUserNameMatches()
    {
        ApplicationUser user = TestDataFactory.CreateUser(Guid.NewGuid().ToString(), "DD", "Dana D");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        ApplicationUser? result = await _repository.FindByUserNameAsync("dd");

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
    }

    [Fact]
    public async Task GetForUpdateAsync_ShouldReturnTrackedUser_WhenExists()
    {
        ApplicationUser user = TestDataFactory.CreateUser(Guid.NewGuid().ToString(), "UPD", "Update User");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        ApplicationUser? result = await _repository.GetForUpdateAsync(Guid.Parse(user.Id));

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
    }

    [Fact]
    public async Task RoleExistsAsync_WhenRoleNameIsWhitespace_ShouldReturnFalse()
    {
        bool result = await _repository.RoleExistsAsync(" ", CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task RoleExistsAsync_WhenCancellationRequested_ShouldReturnCancelledTask()
    {
        using CancellationTokenSource cancellationTokenSource = new();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _repository.RoleExistsAsync("Admin", cancellationTokenSource.Token));
    }

    [Fact]
    public async Task CreateAsync_WhenRoleExists_ShouldReturnSuccess()
    {
        _roleManagerMock.Setup(x => x.RoleExistsAsync("Admin")).ReturnsAsync(true);

        IdentityResult result = await _repository.CreateAsync("Admin");

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task AddToRoleAsync_WhenRoleCreationFails_ShouldReturnFailure()
    {
        ApplicationUser user = TestDataFactory.CreateUser(Guid.NewGuid().ToString(), "E1", "Employee One");

        _roleManagerMock.Setup(x => x.RoleExistsAsync("Supervisor")).ReturnsAsync(false);
        _roleManagerMock.Setup(x => x.CreateAsync(It.IsAny<IdentityRole>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Role create failed" }));

        IdentityResult result = await _repository.AddToRoleAsync(user, "Supervisor");

        Assert.False(result.Succeeded);
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_WhenUserCreationFails_ShouldReturnFailure()
    {
        ApplicationUser user = TestDataFactory.CreateUser(Guid.NewGuid().ToString(), "N1", "New User");

        _userManagerMock.Setup(x => x.CreateAsync(user, "secret"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Create failed" }));

        IdentityResult result = await _repository.AddAsync(user, "secret", null, CancellationToken.None);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task AddAsync_WithRole_ShouldCreateUserAndAssignRole()
    {
        ApplicationUser user = TestDataFactory.CreateUser(Guid.NewGuid().ToString(), "N2", "New User Two");

        _userManagerMock.Setup(x => x.CreateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _roleManagerMock.Setup(x => x.RoleExistsAsync("Admin")).ReturnsAsync(false);
        _roleManagerMock.Setup(x => x.CreateAsync(It.IsAny<IdentityRole>())).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRoleAsync(user, "Admin")).ReturnsAsync(IdentityResult.Success);

        IdentityResult result = await _repository.AddAsync(user, null, "Admin", CancellationToken.None);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task GetRolesAsync_WhenUserIsNull_ShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.GetRolesAsync(null!));
    }

    [Fact]
    public async Task RemoveFromRoleAsync_WhenRoleIsWhitespace_ShouldThrow()
    {
        ApplicationUser user = TestDataFactory.CreateUser(Guid.NewGuid().ToString(), "RM", "Role Remove");

        await Assert.ThrowsAsync<ArgumentException>(() => _repository.RemoveFromRoleAsync(user, " "));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
