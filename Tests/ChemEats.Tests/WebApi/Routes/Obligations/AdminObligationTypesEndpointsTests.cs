using Domain.Entities;
using Domain.Infrastructure.Identity;
using Domain.Repositories.Obligations;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.DTOs.Obligations;
using System.Reflection;
using WebApi.Routes.Obligations;

namespace ChemEats.Tests.WebApi.Routes.Obligations;

public class AdminObligationTypesEndpointsTests
{
    private static readonly ApplicationUser Admin = new() { Id = "admin-id", UserName = "admin" };

    [Fact]
    public async Task GetObligationTypesAsync_ShouldReturnOkWithMappedTypes()
    {
        ObligationType toner = ObligationType.Create("Printer toner");
        Mock<IObligationTypeRepository> repositoryMock = new();
        repositoryMock.Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([toner]);

        IResult result = await InvokePrivateResultMethodAsync(
            "GetObligationTypesAsync",
            repositoryMock.Object,
            CreateMapper(),
            CreateProgramLogger(),
            CancellationToken.None);

        Ok<List<ObligationTypeDto>> okResult = Assert.IsType<Ok<List<ObligationTypeDto>>>(result);
        Assert.Equal(new ObligationTypeDto(toner.Id, "Printer toner"), Assert.Single(okResult.Value!));
    }

    [Fact]
    public async Task CreateObligationTypeAsync_WhenNameIsNew_ShouldAddAndReturnCreated()
    {
        ObligationType? added = null;
        Mock<IObligationTypeRepository> repositoryMock = new();
        repositoryMock.Setup(repository => repository.NameExistsAsync("Printer toner", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repositoryMock.Setup(repository => repository.AddAsync(It.IsAny<ObligationType>(), It.IsAny<CancellationToken>()))
            .Callback<ObligationType, CancellationToken>((type, _) => added = type)
            .Returns(Task.CompletedTask);

        IResult result = await InvokePrivateResultMethodAsync(
            "CreateObligationTypeAsync",
            new SaveObligationTypeDto("Printer toner"),
            repositoryMock.Object,
            CreateUserManagerMock(Admin).Object,
            CreateHttpContext("admin"),
            CreateMapper(),
            CreateProgramLogger(),
            CancellationToken.None);

        Created<ObligationTypeDto> created = Assert.IsType<Created<ObligationTypeDto>>(result);
        Assert.NotNull(added);
        Assert.Equal("Printer toner", created.Value!.Name);
        Assert.Equal($"api/admin/obligation-types/{added.Id}", created.Location);
    }

    [Fact]
    public async Task CreateObligationTypeAsync_WhenNameAlreadyExists_ShouldReturnConflict()
    {
        Mock<IObligationTypeRepository> repositoryMock = new();
        repositoryMock.Setup(repository => repository.NameExistsAsync("Toner", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        IResult result = await InvokePrivateResultMethodAsync(
            "CreateObligationTypeAsync",
            new SaveObligationTypeDto("Toner"),
            repositoryMock.Object,
            CreateUserManagerMock(Admin).Object,
            CreateHttpContext("admin"),
            CreateMapper(),
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "Conflict");
        repositoryMock.Verify(repository => repository.AddAsync(It.IsAny<ObligationType>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateObligationTypeAsync_WhenNameIsEmpty_ShouldReturnBadRequest(string name)
    {
        Mock<IObligationTypeRepository> repositoryMock = new();

        IResult result = await InvokePrivateResultMethodAsync(
            "CreateObligationTypeAsync",
            new SaveObligationTypeDto(name),
            repositoryMock.Object,
            CreateUserManagerMock(Admin).Object,
            CreateHttpContext("admin"),
            CreateMapper(),
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "BadRequest");
        repositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateObligationTypeAsync_WhenAdminIsUnauthorized_ShouldReturnUnauthorized()
    {
        Mock<IObligationTypeRepository> repositoryMock = new();

        IResult result = await InvokePrivateResultMethodAsync(
            "CreateObligationTypeAsync",
            new SaveObligationTypeDto("Toner"),
            repositoryMock.Object,
            CreateUserManagerMock(null).Object,
            CreateHttpContext("admin"),
            CreateMapper(),
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "UnauthorizedHttpResult");
        repositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RenameObligationTypeAsync_WhenTypeExists_ShouldReturnOkWithNewName()
    {
        ObligationType type = ObligationType.Create("Toner");
        type.Rename("Printer toner");

        Mock<IObligationTypeRepository> repositoryMock = new();
        repositoryMock.Setup(repository => repository.NameExistsAsync("Printer toner", type.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repositoryMock.Setup(repository => repository.RenameAsync(type.Id, "Printer toner", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repositoryMock.Setup(repository => repository.GetByIdAsync(type.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        IResult result = await InvokePrivateResultMethodAsync(
            "RenameObligationTypeAsync",
            type.Id,
            new SaveObligationTypeDto("Printer toner"),
            repositoryMock.Object,
            CreateUserManagerMock(Admin).Object,
            CreateHttpContext("admin"),
            CreateMapper(),
            CreateProgramLogger(),
            CancellationToken.None);

        Ok<ObligationTypeDto> okResult = Assert.IsType<Ok<ObligationTypeDto>>(result);
        Assert.Equal("Printer toner", okResult.Value!.Name);
        repositoryMock.VerifyAll();
    }

    [Fact]
    public async Task RenameObligationTypeAsync_WhenTypeDoesNotExist_ShouldReturnNotFound()
    {
        Mock<IObligationTypeRepository> repositoryMock = new();
        repositoryMock.Setup(repository => repository.NameExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repositoryMock.Setup(repository => repository.RenameAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        IResult result = await InvokePrivateResultMethodAsync(
            "RenameObligationTypeAsync",
            Guid.NewGuid(),
            new SaveObligationTypeDto("Toner"),
            repositoryMock.Object,
            CreateUserManagerMock(Admin).Object,
            CreateHttpContext("admin"),
            CreateMapper(),
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "NotFound");
    }

    [Fact]
    public async Task RenameObligationTypeAsync_WhenNameIsTakenByAnotherType_ShouldReturnConflict()
    {
        Guid typeId = Guid.NewGuid();
        Mock<IObligationTypeRepository> repositoryMock = new();
        repositoryMock.Setup(repository => repository.NameExistsAsync("Keyboard", typeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        IResult result = await InvokePrivateResultMethodAsync(
            "RenameObligationTypeAsync",
            typeId,
            new SaveObligationTypeDto("Keyboard"),
            repositoryMock.Object,
            CreateUserManagerMock(Admin).Object,
            CreateHttpContext("admin"),
            CreateMapper(),
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "Conflict");
        repositoryMock.Verify(repository => repository.RenameAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteObligationTypeAsync_WhenTypeExists_ShouldReturnNoContent()
    {
        Guid typeId = Guid.NewGuid();
        Mock<IObligationTypeRepository> repositoryMock = new();
        repositoryMock.Setup(repository => repository.SoftDeleteAsync(typeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        IResult result = await InvokePrivateResultMethodAsync(
            "DeleteObligationTypeAsync",
            typeId,
            repositoryMock.Object,
            CreateUserManagerMock(Admin).Object,
            CreateHttpContext("admin"),
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "NoContent");
        repositoryMock.VerifyAll();
    }

    [Fact]
    public async Task DeleteObligationTypeAsync_WhenTypeDoesNotExist_ShouldReturnNotFound()
    {
        Mock<IObligationTypeRepository> repositoryMock = new();
        repositoryMock.Setup(repository => repository.SoftDeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        IResult result = await InvokePrivateResultMethodAsync(
            "DeleteObligationTypeAsync",
            Guid.NewGuid(),
            repositoryMock.Object,
            CreateUserManagerMock(Admin).Object,
            CreateHttpContext("admin"),
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "NotFound");
    }

    private static IMapper CreateMapper()
    {
        Mock<IMapper> mapperMock = new();
        mapperMock.Setup(mapper => mapper.Map<ObligationTypeDto>(It.IsAny<ObligationType>()))
            .Returns((object source) => new ObligationTypeDto(((ObligationType)source).Id, ((ObligationType)source).Name));
        return mapperMock.Object;
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock(ApplicationUser? currentUser)
    {
        Mock<IUserStore<ApplicationUser>> userStoreMock = new();
        Mock<UserManager<ApplicationUser>> userManagerMock = new(
            userStoreMock.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);

        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(currentUser);

        return userManagerMock;
    }

    private static async Task<IResult> InvokePrivateResultMethodAsync(string methodName, params object?[] parameters)
    {
        MethodInfo? methodInfo = typeof(AdminObligationTypesEndpoints)
            .GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(methodInfo);

        object? invocationResult = methodInfo.Invoke(null, parameters);
        Task<IResult> task = Assert.IsType<Task<IResult>>(invocationResult);
        return await task;
    }

    private static object CreateProgramLogger()
    {
        Type programType = typeof(AdminObligationTypesEndpoints).Assembly.GetType("Program")
            ?? throw new InvalidOperationException("Program type not found.");

        ILoggerFactory loggerFactory = LoggerFactory.Create(_ => { });
        Type loggerType = typeof(Logger<>).MakeGenericType(programType);

        return Activator.CreateInstance(loggerType, loggerFactory)
            ?? throw new InvalidOperationException("Unable to create program logger.");
    }

    private static DefaultHttpContext CreateHttpContext(string userName)
    {
        DefaultHttpContext httpContext = new();
        httpContext.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
            [
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, userName)
            ],
            "TestAuth"));
        return httpContext;
    }

    private static void AssertHttpResultType(IResult result, string expectedTypeName)
    {
        Assert.Contains(expectedTypeName, result.GetType().Name, StringComparison.Ordinal);
    }
}
