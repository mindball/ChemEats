using System.Reflection;
using Domain.Common.Enums;
using Domain.Entities;
using Domain.Infrastructure.Identity;
using Domain.Repositories.Employees;
using Domain.Repositories.Suppliers;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.DTOs.Suppliers;
using WebApi.Routes.Suppliers;

namespace ChemEats.Tests.WebApi.Routes.Suppliers;

public class SupplierEndpointsTests
{
    [Fact]
    public async Task GetSupplierByIdAsync_WhenSupplierMissing_ShouldReturnNotFound()
    {
        Mock<ISupplierRepository> supplierRepositoryMock = new();
        supplierRepositoryMock.Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Supplier?)null);

        Mock<IMapper> mapperMock = new();
        object logger = CreateProgramLogger();

        IResult result = await InvokePrivateResultMethodAsync(
            "GetSupplierByIdAsync",
            Guid.NewGuid(),
            supplierRepositoryMock.Object,
            mapperMock.Object,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "NotFound");
    }

    [Fact]
    public async Task CreateSupplierAsync_WhenSupervisorIsProvided_ShouldAssignSupervisorRoleAndCreateSupplier()
    {
        Guid supervisorGuid = Guid.NewGuid();
        string supervisorId = supervisorGuid.ToString();

        CreateSupplierDto dto = new()
        {
            Name = "Supplier A",
            VatNumber = "BG123",
            SupervisorId = supervisorId
        };

        Supplier mappedSupplier = new(Guid.NewGuid(), "Supplier A", "BG123", PaymentTerms.Net10);
        SupplierDto mappedDto = new() { Id = mappedSupplier.Id, Name = mappedSupplier.Name, VatNumber = mappedSupplier.VatNumber };

        Mock<IMapper> mapperMock = new();
        mapperMock.Setup(mapper => mapper.Map<Supplier>(dto)).Returns(mappedSupplier);
        mapperMock.Setup(mapper => mapper.Map<SupplierDto>(mappedSupplier)).Returns(mappedDto);

        Mock<ISupplierRepository> supplierRepositoryMock = new();

        ApplicationUser supervisorUser = new() { Id = supervisorId, UserName = "SUP" };

        Mock<IUserRepository> userRepositoryMock = new();
        userRepositoryMock.Setup(repository => repository.GetByIdAsync(supervisorGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(supervisorUser);
        userRepositoryMock.Setup(repository => repository.GetRolesAsync(supervisorUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["Employee"]);
        userRepositoryMock.Setup(repository => repository.AddToRoleAsync(supervisorUser, "Supervisor", It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResult.Success);

        object logger = CreateProgramLogger();
        DefaultHttpContext httpContext = CreateHttpContext("admin-user");

        IResult result = await InvokePrivateResultMethodAsync(
            "CreateSupplierAsync",
            dto,
            supplierRepositoryMock.Object,
            userRepositoryMock.Object,
            mapperMock.Object,
            logger,
            httpContext,
            CancellationToken.None);

        AssertHttpResultType(result, "Created");
        supplierRepositoryMock.Verify(repository => repository.AddAsync(mappedSupplier, It.IsAny<CancellationToken>()), Times.Once);
        userRepositoryMock.Verify(repository => repository.AddToRoleAsync(supervisorUser, "Supervisor", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateSupplierAsync_WhenSupplierMissing_ShouldReturnNotFound()
    {
        Mock<ISupplierRepository> supplierRepositoryMock = new();
        supplierRepositoryMock.Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Supplier?)null);

        Mock<IUserRepository> userRepositoryMock = new();
        Mock<IMapper> mapperMock = new();
        object logger = CreateProgramLogger();
        DefaultHttpContext httpContext = CreateHttpContext("admin-user");

        UpdateSupplierDto dto = new() { Name = "Updated", VatNumber = "BG123" };

        IResult result = await InvokePrivateResultMethodAsync(
            "UpdateSupplierAsync",
            Guid.NewGuid(),
            dto,
            supplierRepositoryMock.Object,
            userRepositoryMock.Object,
            mapperMock.Object,
            logger,
            httpContext,
            CancellationToken.None);

        AssertHttpResultType(result, "NotFound");
    }

    [Fact]
    public async Task DeleteSupplierAsync_WhenSupplierMissing_ShouldReturnNotFound()
    {
        Mock<ISupplierRepository> supplierRepositoryMock = new();
        supplierRepositoryMock.Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Supplier?)null);

        object logger = CreateProgramLogger();
        DefaultHttpContext httpContext = CreateHttpContext("admin-user");

        IResult result = await InvokePrivateResultMethodAsync(
            "DeleteSupplierAsync",
            Guid.NewGuid(),
            supplierRepositoryMock.Object,
            logger,
            httpContext,
            CancellationToken.None);

        AssertHttpResultType(result, "NotFound");
    }

    [Fact]
    public async Task UpdateSupplierAsync_WhenSupervisorRemoved_ShouldPersistSupplierWithoutSupervisor()
    {
        Guid supplierId = Guid.NewGuid();
        Supplier existing = new(supplierId, "Before", "BG123", PaymentTerms.Net10);
        existing.AssignSupervisor(Guid.NewGuid().ToString());

        UpdateSupplierDto dto = new()
        {
            Name = "After",
            VatNumber = "BG123",
            SupervisorId = null
        };

        Mock<ISupplierRepository> supplierRepositoryMock = new();
        supplierRepositoryMock.Setup(repository => repository.GetByIdAsync(supplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        Mock<IMapper> mapperMock = new();
        mapperMock.Setup(mapper => mapper.Map(dto, existing))
            .Callback(() =>
            {
                Supplier replacement = new(supplierId, "After", "BG123", PaymentTerms.Net10);
                replacement.RemoveSupervisor();
            });
        mapperMock.Setup(mapper => mapper.Map<SupplierDto>(existing))
            .Returns(new SupplierDto { Id = existing.Id, Name = existing.Name, VatNumber = existing.VatNumber });

        Mock<IUserRepository> userRepositoryMock = new();

        object logger = CreateProgramLogger();
        DefaultHttpContext httpContext = CreateHttpContext("admin-user");

        IResult result = await InvokePrivateResultMethodAsync(
            "UpdateSupplierAsync",
            supplierId,
            dto,
            supplierRepositoryMock.Object,
            userRepositoryMock.Object,
            mapperMock.Object,
            logger,
            httpContext,
            CancellationToken.None);

        AssertHttpResultType(result, "Ok");
        supplierRepositoryMock.Verify(repository => repository.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Null(existing.SupervisorId);
    }

    private static async Task<IResult> InvokePrivateResultMethodAsync(string methodName, params object[] parameters)
    {
        MethodInfo? methodInfo = typeof(SupplierEndpoints)
            .GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(methodInfo);

        object? invocationResult = methodInfo.Invoke(null, parameters);
        Task<IResult> task = Assert.IsType<Task<IResult>>(invocationResult);
        return await task;
    }

    private static object CreateProgramLogger()
    {
        Type programType = typeof(SupplierEndpoints).Assembly.GetType("Program")
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
