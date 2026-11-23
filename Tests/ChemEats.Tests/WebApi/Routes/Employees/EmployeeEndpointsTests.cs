using System.Reflection;
using Domain.Infrastructure.Identity;
using Domain.Repositories.Employees;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.DTOs.Employees;
using WebApi.Infrastructure.Identity;
using WebApi.Routes.Employees;

namespace ChemEats.Tests.WebApi.Routes.Employees;

public class EmployeeEndpointsTests
{
    [Fact]
    public async Task CreateEmployeeAsync_WhenRequestIsValid_ShouldCreateUserWithAbbreviationAsPassword()
    {
        Mock<IUserRepository> userRepositoryMock = new();
        userRepositoryMock
            .Setup(repository => repository.FindByUserNameAsync("MM", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);
        ApplicationUser? createdUser = null;
        userRepositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<ApplicationUser>(), "MM", "Employee", It.IsAny<CancellationToken>()))
            .Callback<ApplicationUser, string?, string?, CancellationToken>((user, _, _, _) => createdUser = user)
            .ReturnsAsync(IdentityResult.Success);

        IResult result = await InvokePrivateResultMethodAsync(
            "CreateEmployeeAsync",
            new CreateEmployeeRequestDto(" mm ", "Mincho M", null),
            userRepositoryMock.Object,
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "Created");
        Assert.NotNull(createdUser);
        Assert.Equal("MM", createdUser.Abbreviation);
        Assert.Equal("MM", createdUser.UserName);
        Assert.Equal("Mincho M", createdUser.FullName);
    }

    [Fact]
    public async Task CreateEmployeeAsync_WhenAbbreviationExists_ShouldReturnConflict()
    {
        Mock<IUserRepository> userRepositoryMock = new();
        userRepositoryMock
            .Setup(repository => repository.FindByUserNameAsync("MM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationUser.CreateEmployee("MM", "Mincho", null));

        IResult result = await InvokePrivateResultMethodAsync(
            "CreateEmployeeAsync",
            new CreateEmployeeRequestDto("MM", "Another", null),
            userRepositoryMock.Object,
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "Conflict");
        userRepositoryMock.Verify(repository => repository.AddAsync(
            It.IsAny<ApplicationUser>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("", "Name", null)]
    [InlineData("ABCD", "Name", null)]
    [InlineData("MM", " ", null)]
    [InlineData("MM", "Name", "not-an-email")]
    public async Task CreateEmployeeAsync_WhenRequestIsInvalid_ShouldReturnBadRequest(string abbreviation, string fullName, string? email)
    {
        Mock<IUserRepository> userRepositoryMock = new();

        IResult result = await InvokePrivateResultMethodAsync(
            "CreateEmployeeAsync",
            new CreateEmployeeRequestDto(abbreviation, fullName, email),
            userRepositoryMock.Object,
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "BadRequest");
    }

    [Fact]
    public async Task AssignRoleAsync_WhenUserIdIsInvalid_ShouldReturnBadRequest()
    {
        Mock<IUserRepository> userRepositoryMock = new();
        object logger = CreateProgramLogger();

        IResult result = await InvokePrivateResultMethodAsync(
            "AssignRoleAsync",
            "not-guid",
            "Admin",
            userRepositoryMock.Object,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "BadRequest");
    }

    [Fact]
    public async Task AssignRoleAsync_WhenUserIsMissing_ShouldReturnNotFound()
    {
        Mock<IUserRepository> userRepositoryMock = new();
        userRepositoryMock.Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        object logger = CreateProgramLogger();

        IResult result = await InvokePrivateResultMethodAsync(
            "AssignRoleAsync",
            Guid.NewGuid().ToString(),
            "Admin",
            userRepositoryMock.Object,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "NotFound");
    }

    [Fact]
    public async Task RemoveRoleAsync_WhenUserDoesNotHaveRole_ShouldReturnOk()
    {
        ApplicationUser user = new() { Id = Guid.NewGuid().ToString(), UserName = "MM" };

        Mock<IUserRepository> userRepositoryMock = new();
        userRepositoryMock.Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        userRepositoryMock.Setup(repository => repository.GetRolesAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["Employee"]);

        object logger = CreateProgramLogger();

        IResult result = await InvokePrivateResultMethodAsync(
            "RemoveRoleAsync",
            user.Id,
            "Admin",
            userRepositoryMock.Object,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "Ok");
    }

    [Fact]
    public async Task LoginAsync_WhenUserIsMissing_ShouldReturnUnauthorized()
    {
        LoginRequest request = new()
        {
            Email = "missing@cpachem.com",
            Password = "123"
        };

        Mock<IUserRepository> userRepositoryMock = new();
        userRepositoryMock.Setup(repository => repository.FindByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        Mock<SignInManager<ApplicationUser>> signInManagerMock = CreateSignInManagerMock();
        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        JwtTokenProvider tokenProvider = new(new JwtSettings("issuer", "audience", "super_secret_key_that_is_long_enough_123", 60));
        Mock<ILogger<JwtTokenProvider>> loggerMock = new();

        IResult result = await InvokePrivateResultMethodAsync(
            "LoginAsync",
            request,
            userRepositoryMock.Object,
            signInManagerMock.Object,
            userManagerMock.Object,
            tokenProvider,
            loggerMock.Object);

        AssertHttpResultType(result, "UnauthorizedHttpResult");
    }

    private static async Task<IResult> InvokePrivateResultMethodAsync(string methodName, params object[] parameters)
    {
        MethodInfo? methodInfo = typeof(EmployeeEndPoints)
            .GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(methodInfo);

        object? invocationResult = methodInfo.Invoke(null, parameters);
        Task<IResult> task = Assert.IsType<Task<IResult>>(invocationResult);
        return await task;
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        Mock<IUserStore<ApplicationUser>> userStoreMock = new();
        return new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }

    private static Mock<SignInManager<ApplicationUser>> CreateSignInManagerMock()
    {
        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        Mock<IHttpContextAccessor> contextAccessorMock = new();
        Mock<IUserClaimsPrincipalFactory<ApplicationUser>> claimsFactoryMock = new();

        return new Mock<SignInManager<ApplicationUser>>(
            userManagerMock.Object,
            contextAccessorMock.Object,
            claimsFactoryMock.Object,
            null!,
            null!,
            null!,
            null!);
    }

    private static void AssertHttpResultType(IResult result, string expectedTypeName)
    {
        Assert.Contains(expectedTypeName, result.GetType().Name, StringComparison.Ordinal);
    }

    private static object CreateProgramLogger()
    {
        Type programType = typeof(EmployeeEndPoints).Assembly.GetType("Program")
            ?? throw new InvalidOperationException("Program type not found.");

        ILoggerFactory loggerFactory = LoggerFactory.Create(_ => { });
        Type loggerType = typeof(Logger<>).MakeGenericType(programType);

        return Activator.CreateInstance(loggerType, loggerFactory)
            ?? throw new InvalidOperationException("Unable to create program logger.");
    }
}
