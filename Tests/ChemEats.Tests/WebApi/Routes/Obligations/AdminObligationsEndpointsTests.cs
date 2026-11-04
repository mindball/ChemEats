using Domain.Entities;
using Domain.Infrastructure.Identity;
using Domain.Models.Obligations;
using Domain.Repositories.Obligations;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Common.Enums;
using Shared.DTOs.Obligations;
using System.Reflection;
using WebApi.Routes.Obligations;

namespace ChemEats.Tests.WebApi.Routes.Obligations;

public class AdminObligationsEndpointsTests
{
    private static readonly ApplicationUser Admin = new() { Id = "admin-id", UserName = "admin" };
    private static readonly ApplicationUser Employee = new() { Id = "user-1", UserName = "U1" };

    private static readonly ObligationType Toner = ObligationType.Create("Printer toner");

    [Fact]
    public async Task CreateObligationAsync_WhenRequestIsValid_ShouldCreateFromTypeAsAdminAndReturnCreated()
    {
        CreateOtherObligationDto request = new("user-1", Toner.Id, 12.5m, new DateTime(2026, 11, 2));
        OtherObligation? added = null;

        Mock<IOtherObligationRepository> repositoryMock = new();
        repositoryMock.Setup(repository => repository.AddAsync(It.IsAny<OtherObligation>(), It.IsAny<CancellationToken>()))
            .Callback<OtherObligation, CancellationToken>((obligation, _) => added = obligation)
            .Returns(Task.CompletedTask);
        repositoryMock.Setup(repository => repository.GetItemByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => CreateItem(id));

        Mock<IMapper> mapperMock = new();
        mapperMock.Setup(mapper => mapper.Map<OtherObligationDto>(It.IsAny<OtherObligationItem>()))
            .Returns((object source) => CreateDto(((OtherObligationItem)source).Id));

        IResult result = await InvokePrivateResultMethodAsync(
            "CreateObligationAsync",
            request,
            repositoryMock.Object,
            CreateTypeRepositoryMock(Toner).Object,
            CreateUserManagerMock(Admin, Employee).Object,
            CreateHttpContext("admin"),
            mapperMock.Object,
            CreateProgramLogger(),
            CancellationToken.None);

        Created<OtherObligationDto> created = Assert.IsType<Created<OtherObligationDto>>(result);
        Assert.NotNull(added);
        Assert.Equal("user-1", added.UserId);
        Assert.Equal(Toner.Id, added.ObligationTypeId);
        Assert.Equal("Printer toner", added.Description);
        Assert.Equal("admin-id", added.CreatedByUserId);
        Assert.Equal(12.5m, added.Amount);
        Assert.Equal(added.Id, created.Value!.Id);
        Assert.Equal($"api/admin/obligations/{added.Id}", created.Location);
    }

    [Theory]
    [InlineData("", true, 10)]
    [InlineData("user-1", false, 10)]
    [InlineData("user-1", true, 0)]
    [InlineData("user-1", true, -1)]
    public async Task CreateObligationAsync_WhenRequestIsInvalid_ShouldReturnBadRequest(string userId, bool hasType, int amount)
    {
        CreateOtherObligationDto request = new(userId, hasType ? Toner.Id : Guid.Empty, amount, DateTime.Today);
        Mock<IOtherObligationRepository> repositoryMock = new();

        IResult result = await InvokePrivateResultMethodAsync(
            "CreateObligationAsync",
            request,
            repositoryMock.Object,
            CreateTypeRepositoryMock(Toner).Object,
            CreateUserManagerMock(Admin, Employee).Object,
            CreateHttpContext("admin"),
            new Mock<IMapper>().Object,
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "BadRequest");
        repositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateObligationAsync_WhenTypeDoesNotExist_ShouldReturnBadRequest()
    {
        CreateOtherObligationDto request = new("user-1", Guid.NewGuid(), 10m, DateTime.Today);
        Mock<IOtherObligationRepository> repositoryMock = new();

        IResult result = await InvokePrivateResultMethodAsync(
            "CreateObligationAsync",
            request,
            repositoryMock.Object,
            CreateTypeRepositoryMock(Toner).Object,
            CreateUserManagerMock(Admin, Employee).Object,
            CreateHttpContext("admin"),
            new Mock<IMapper>().Object,
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "BadRequest");
        repositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateObligationAsync_WhenBodyIsMissing_ShouldReturnBadRequest()
    {
        IResult result = await InvokePrivateResultMethodAsync(
            "CreateObligationAsync",
            null,
            new Mock<IOtherObligationRepository>().Object,
            CreateTypeRepositoryMock(Toner).Object,
            CreateUserManagerMock(Admin, Employee).Object,
            CreateHttpContext("admin"),
            new Mock<IMapper>().Object,
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "BadRequest");
    }

    [Fact]
    public async Task CreateObligationAsync_WhenEmployeeDoesNotExist_ShouldReturnNotFound()
    {
        CreateOtherObligationDto request = new("missing-user", Toner.Id, 10m, DateTime.Today);
        Mock<IOtherObligationRepository> repositoryMock = new();

        IResult result = await InvokePrivateResultMethodAsync(
            "CreateObligationAsync",
            request,
            repositoryMock.Object,
            CreateTypeRepositoryMock(Toner).Object,
            CreateUserManagerMock(Admin, Employee).Object,
            CreateHttpContext("admin"),
            new Mock<IMapper>().Object,
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "NotFound");
        repositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateObligationAsync_WhenAdminIsUnauthorized_ShouldReturnUnauthorized()
    {
        CreateOtherObligationDto request = new("user-1", Toner.Id, 10m, DateTime.Today);
        Mock<IOtherObligationRepository> repositoryMock = new();

        IResult result = await InvokePrivateResultMethodAsync(
            "CreateObligationAsync",
            request,
            repositoryMock.Object,
            CreateTypeRepositoryMock(Toner).Object,
            CreateUserManagerMock(null, Employee).Object,
            CreateHttpContext("admin"),
            new Mock<IMapper>().Object,
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "UnauthorizedHttpResult");
        repositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateObligationAsync_WhenObligationExists_ShouldUpdateWithTypeAndReturnOk()
    {
        Guid obligationId = Guid.NewGuid();
        UpdateOtherObligationDto request = new(Toner.Id, 30m, new DateTime(2026, 10, 7));

        Mock<IOtherObligationRepository> repositoryMock = new();
        repositoryMock.Setup(repository => repository.UpdateAsync(
                obligationId,
                It.Is<ObligationType>(type => type.Id == Toner.Id),
                30m,
                new DateTime(2026, 10, 7),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repositoryMock.Setup(repository => repository.GetItemByIdAsync(obligationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateItem(obligationId));

        Mock<IMapper> mapperMock = new();
        mapperMock.Setup(mapper => mapper.Map<OtherObligationDto>(It.IsAny<OtherObligationItem>()))
            .Returns(CreateDto(obligationId));

        IResult result = await InvokePrivateResultMethodAsync(
            "UpdateObligationAsync",
            obligationId,
            request,
            repositoryMock.Object,
            CreateTypeRepositoryMock(Toner).Object,
            CreateUserManagerMock(Admin).Object,
            CreateHttpContext("admin"),
            mapperMock.Object,
            CreateProgramLogger(),
            CancellationToken.None);

        Ok<OtherObligationDto> okResult = Assert.IsType<Ok<OtherObligationDto>>(result);
        Assert.Equal(obligationId, okResult.Value!.Id);
        repositoryMock.VerifyAll();
    }

    [Fact]
    public async Task UpdateObligationAsync_WhenObligationDoesNotExist_ShouldReturnNotFound()
    {
        Mock<IOtherObligationRepository> repositoryMock = new();
        repositoryMock.Setup(repository => repository.UpdateAsync(It.IsAny<Guid>(), It.IsAny<ObligationType>(), It.IsAny<decimal>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        IResult result = await InvokePrivateResultMethodAsync(
            "UpdateObligationAsync",
            Guid.NewGuid(),
            new UpdateOtherObligationDto(Toner.Id, 30m, DateTime.Today),
            repositoryMock.Object,
            CreateTypeRepositoryMock(Toner).Object,
            CreateUserManagerMock(Admin).Object,
            CreateHttpContext("admin"),
            new Mock<IMapper>().Object,
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "NotFound");
    }

    [Fact]
    public async Task UpdateObligationAsync_WhenAmountIsNotPositive_ShouldReturnBadRequest()
    {
        Mock<IOtherObligationRepository> repositoryMock = new();

        IResult result = await InvokePrivateResultMethodAsync(
            "UpdateObligationAsync",
            Guid.NewGuid(),
            new UpdateOtherObligationDto(Toner.Id, 0m, DateTime.Today),
            repositoryMock.Object,
            CreateTypeRepositoryMock(Toner).Object,
            CreateUserManagerMock(Admin).Object,
            CreateHttpContext("admin"),
            new Mock<IMapper>().Object,
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "BadRequest");
        repositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateObligationAsync_WhenTypeDoesNotExist_ShouldReturnBadRequest()
    {
        Mock<IOtherObligationRepository> repositoryMock = new();

        IResult result = await InvokePrivateResultMethodAsync(
            "UpdateObligationAsync",
            Guid.NewGuid(),
            new UpdateOtherObligationDto(Guid.NewGuid(), 10m, DateTime.Today),
            repositoryMock.Object,
            CreateTypeRepositoryMock(Toner).Object,
            CreateUserManagerMock(Admin).Object,
            CreateHttpContext("admin"),
            new Mock<IMapper>().Object,
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "BadRequest");
        repositoryMock.VerifyNoOtherCalls();
    }

    private static Mock<IObligationTypeRepository> CreateTypeRepositoryMock(params ObligationType[] knownTypes)
    {
        Mock<IObligationTypeRepository> typeRepositoryMock = new();
        typeRepositoryMock.Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => knownTypes.FirstOrDefault(type => type.Id == id));
        return typeRepositoryMock;
    }

    [Fact]
    public async Task DeleteObligationAsync_WhenObligationExists_ShouldReturnNoContent()
    {
        Guid obligationId = Guid.NewGuid();
        Mock<IOtherObligationRepository> repositoryMock = new();
        repositoryMock.Setup(repository => repository.SoftDeleteAsync(obligationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        IResult result = await InvokePrivateResultMethodAsync(
            "DeleteObligationAsync",
            obligationId,
            repositoryMock.Object,
            CreateUserManagerMock(Admin).Object,
            CreateHttpContext("admin"),
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "NoContent");
        repositoryMock.VerifyAll();
    }

    [Fact]
    public async Task DeleteObligationAsync_WhenObligationDoesNotExist_ShouldReturnNotFound()
    {
        Mock<IOtherObligationRepository> repositoryMock = new();
        repositoryMock.Setup(repository => repository.SoftDeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        IResult result = await InvokePrivateResultMethodAsync(
            "DeleteObligationAsync",
            Guid.NewGuid(),
            repositoryMock.Object,
            CreateUserManagerMock(Admin).Object,
            CreateHttpContext("admin"),
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "NotFound");
    }

    [Fact]
    public async Task PayObligationsAsync_WhenRequestIsValid_ShouldPayAsAdminAndReturnOk()
    {
        Guid obligationId = Guid.NewGuid();
        Mock<IOtherObligationRepository> repositoryMock = new();
        repositoryMock.Setup(repository => repository.MarkAsPaidAsync(
                "user-1",
                It.Is<IReadOnlyList<Guid>>(ids => ids.Single() == obligationId),
                It.IsAny<DateTime>(),
                "admin-id",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((1, 12.5m));

        IResult result = await InvokePrivateResultMethodAsync(
            "PayObligationsAsync",
            new ObligationPayRequestDto("user-1", [obligationId]),
            repositoryMock.Object,
            CreateUserManagerMock(Admin).Object,
            CreateHttpContext("admin"),
            CreateProgramLogger(),
            CancellationToken.None);

        Ok<ObligationPayResponseDto> okResult = Assert.IsType<Ok<ObligationPayResponseDto>>(result);
        Assert.Equal(new ObligationPayResponseDto(1, 12.5m), okResult.Value);
        repositoryMock.VerifyAll();
    }

    [Fact]
    public async Task PayObligationsAsync_WhenIdsAreEmpty_ShouldReturnBadRequest()
    {
        Mock<IOtherObligationRepository> repositoryMock = new();

        IResult result = await InvokePrivateResultMethodAsync(
            "PayObligationsAsync",
            new ObligationPayRequestDto("user-1", []),
            repositoryMock.Object,
            CreateUserManagerMock(Admin).Object,
            CreateHttpContext("admin"),
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "BadRequest");
        repositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PayAllUnpaidObligationsAsync_WhenRequestIsValid_ShouldPayUpToDateAndReturnOk()
    {
        DateTime upToDate = new(2026, 10, 31);
        Mock<IOtherObligationRepository> repositoryMock = new();
        repositoryMock.Setup(repository => repository.MarkAllUnpaidAsPaidAsync(
                "user-1",
                upToDate,
                It.IsAny<DateTime>(),
                "admin-id",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((3, 40m));

        IResult result = await InvokePrivateResultMethodAsync(
            "PayAllUnpaidObligationsAsync",
            new ObligationPayAllRequestDto("user-1", upToDate),
            repositoryMock.Object,
            CreateUserManagerMock(Admin).Object,
            CreateHttpContext("admin"),
            CreateProgramLogger(),
            CancellationToken.None);

        Ok<ObligationPayResponseDto> okResult = Assert.IsType<Ok<ObligationPayResponseDto>>(result);
        Assert.Equal(new ObligationPayResponseDto(3, 40m), okResult.Value);
        repositoryMock.VerifyAll();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PayAllUnpaidObligationsAsync_WhenUserIdIsMissing_ShouldReturnBadRequest(string userId)
    {
        Mock<IOtherObligationRepository> repositoryMock = new();

        IResult result = await InvokePrivateResultMethodAsync(
            "PayAllUnpaidObligationsAsync",
            new ObligationPayAllRequestDto(userId, null),
            repositoryMock.Object,
            CreateUserManagerMock(Admin).Object,
            CreateHttpContext("admin"),
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "BadRequest");
        repositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetUserObligationsAsync_ShouldReturnOkWithMappedItems()
    {
        Guid obligationId = Guid.NewGuid();
        Mock<IOtherObligationRepository> repositoryMock = new();
        repositoryMock.Setup(repository => repository.GetForUserAsync("user-1", null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateItem(obligationId)]);

        Mock<IMapper> mapperMock = new();
        mapperMock.Setup(mapper => mapper.Map<OtherObligationDto>(It.IsAny<OtherObligationItem>()))
            .Returns(CreateDto(obligationId));

        IResult result = await InvokePrivateResultMethodAsync(
            "GetUserObligationsAsync",
            "user-1",
            null,
            null,
            repositoryMock.Object,
            mapperMock.Object,
            CreateProgramLogger(),
            CancellationToken.None);

        Ok<List<OtherObligationDto>> okResult = Assert.IsType<Ok<List<OtherObligationDto>>>(result);
        Assert.Equal(obligationId, Assert.Single(okResult.Value!).Id);
    }

    [Fact]
    public async Task GetUserObligationsAsync_WhenEndDateIsBeforeStartDate_ShouldReturnBadRequest()
    {
        Mock<IOtherObligationRepository> repositoryMock = new();

        IResult result = await InvokePrivateResultMethodAsync(
            "GetUserObligationsAsync",
            "user-1",
            new DateTime(2026, 10, 31),
            new DateTime(2026, 10, 1),
            repositoryMock.Object,
            new Mock<IMapper>().Object,
            CreateProgramLogger(),
            CancellationToken.None);

        AssertHttpResultType(result, "BadRequest");
        repositoryMock.VerifyNoOtherCalls();
    }

    private static OtherObligationItem CreateItem(Guid id) =>
        new(id, "user-1", "User One", Toner.Id, "Printer toner", 12.5m, new DateTime(2026, 11, 2), DateTime.Now, "Admin", PaymentStatus.Unpaid, null, null);

    private static OtherObligationDto CreateDto(Guid id) =>
        new(id, "user-1", "User One", Toner.Id, "Printer toner", 12.5m, new DateTime(2026, 11, 2), DateTime.Now, "Admin", PaymentStatusDto.Unpaid, null, null);

    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock(ApplicationUser? currentUser, params ApplicationUser[] knownUsers)
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
        userManagerMock.Setup(manager => manager.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => knownUsers.FirstOrDefault(user => user.Id == id));

        return userManagerMock;
    }

    private static async Task<IResult> InvokePrivateResultMethodAsync(string methodName, params object?[] parameters)
    {
        MethodInfo? methodInfo = typeof(AdminObligationsEndpoints)
            .GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(methodInfo);

        object? invocationResult = methodInfo.Invoke(null, parameters);
        Task<IResult> task = Assert.IsType<Task<IResult>>(invocationResult);
        return await task;
    }

    private static object CreateProgramLogger()
    {
        Type programType = typeof(AdminObligationsEndpoints).Assembly.GetType("Program")
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
