using System.Reflection;
using Domain.Common.Enums;
using Domain.Entities;
using Domain.Infrastructure.Exceptions;
using Domain.Repositories.MealOrders;
using Domain.Repositories.Menus;
using Domain.Repositories.Suppliers;
using MapsterMapper;
using MenuParser.Abstractions;
using MenuParser.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.DTOs.Meals;
using Shared.DTOs.Menus;
using WebApi.Routes.Menus;

namespace ChemEats.Tests.WebApi.Routes.Menus;

public class MenuEndpointsTests
{
    [Fact]
    public async Task GetMenuByIdAsync_WhenMenuMissing_ShouldReturnNotFound()
    {
        Mock<IMenuRepository> menuRepositoryMock = new();
        menuRepositoryMock.Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Menu?)null);

        Mock<IMealOrderRepository> orderRepositoryMock = new();
        Mock<IMapper> mapperMock = new();
        object logger = CreateProgramLogger();

        IResult result = await InvokePrivateResultMethodAsync(
            "GetMenuByIdAsync",
            Guid.NewGuid(),
            menuRepositoryMock.Object,
            orderRepositoryMock.Object,
            mapperMock.Object,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "NotFound");
    }

    [Fact]
    public async Task CreateMenuAsync_WhenSupplierMissing_ShouldReturnNotFound()
    {
        CreateMenuDto dto = CreateValidCreateMenuDto();

        Mock<IMenuRepository> menuRepositoryMock = new();
        Mock<ISupplierRepository> supplierRepositoryMock = new();
        supplierRepositoryMock.Setup(repository => repository.GetByIdAsync(dto.SupplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Supplier?)null);

        Mock<IMapper> mapperMock = new();
        object logger = CreateProgramLogger();

        IResult result = await InvokePrivateResultMethodAsync(
            "CreateMenuAsync",
            dto,
            menuRepositoryMock.Object,
            supplierRepositoryMock.Object,
            mapperMock.Object,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "NotFound");
    }

    [Fact]
    public async Task CreateMenuAsync_WhenMenuAlreadyExists_ShouldReturnConflict()
    {
        CreateMenuDto dto = CreateValidCreateMenuDto();
        Supplier supplier = new(dto.SupplierId, "Supplier", "BG123", PaymentTerms.Net10);

        Mock<IMenuRepository> menuRepositoryMock = new();
        menuRepositoryMock.Setup(repository => repository.ExistsAsync(dto.SupplierId, dto.Date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Mock<ISupplierRepository> supplierRepositoryMock = new();
        supplierRepositoryMock.Setup(repository => repository.GetByIdAsync(dto.SupplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(supplier);

        Mock<IMapper> mapperMock = new();
        object logger = CreateProgramLogger();

        IResult result = await InvokePrivateResultMethodAsync(
            "CreateMenuAsync",
            dto,
            menuRepositoryMock.Object,
            supplierRepositoryMock.Object,
            mapperMock.Object,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "Conflict");
    }

    [Fact]
    public async Task CreateMenuAsync_WhenDomainValidationFails_ShouldReturnBadRequest()
    {
        Guid supplierId = Guid.NewGuid();
        CreateMenuDto dto = new(
            supplierId,
            DateTime.Today.AddDays(2),
            DateTime.Today.AddDays(1).AddHours(12),
            [new CreateMealDto(" ", 10m)]);

        Supplier supplier = new(supplierId, "Supplier", "BG123", PaymentTerms.Net10);

        Mock<IMenuRepository> menuRepositoryMock = new();
        menuRepositoryMock.Setup(repository => repository.ExistsAsync(dto.SupplierId, dto.Date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Mock<ISupplierRepository> supplierRepositoryMock = new();
        supplierRepositoryMock.Setup(repository => repository.GetByIdAsync(dto.SupplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(supplier);

        Mock<IMapper> mapperMock = new();
        object logger = CreateProgramLogger();

        IResult result = await InvokePrivateResultMethodAsync(
            "CreateMenuAsync",
            dto,
            menuRepositoryMock.Object,
            supplierRepositoryMock.Object,
            mapperMock.Object,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "BadRequest");
    }

    [Fact]
    public async Task ParseMenuFileAsync_WhenNoFile_ShouldReturnBadRequest()
    {
        Mock<IMenuFileParser> parserMock = new();
        object logger = CreateProgramLogger();

        IResult result = await InvokePrivateResultMethodAsync(
            "ParseMenuFileAsync",
            null,
            parserMock.Object,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "BadRequest");
    }

    [Fact]
    public async Task ParseMenuFileAsync_WhenFileFormatNotSupported_ShouldReturnBadRequest()
    {
        IFormFile file = CreateFile("menu.txt", "a,b");
        Mock<IMenuFileParser> parserMock = new();
        parserMock.Setup(parser => parser.IsSupported("menu.txt")).Returns(false);

        object logger = CreateProgramLogger();

        IResult result = await InvokePrivateResultMethodAsync(
            "ParseMenuFileAsync",
            file,
            parserMock.Object,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "BadRequest");
    }

    [Fact]
    public async Task ParseMenuFileAsync_WhenValidFile_ShouldReturnOk()
    {
        IFormFile file = CreateFile("menu.csv", "Soup,10");
        Mock<IMenuFileParser> parserMock = new();
        parserMock.Setup(parser => parser.IsSupported("menu.csv")).Returns(true);
        parserMock.Setup(parser => parser.ParseAsync(It.IsAny<Stream>(), "menu.csv", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ParsedMeal("Soup", 10m)]);

        object logger = CreateProgramLogger();

        IResult result = await InvokePrivateResultMethodAsync(
            "ParseMenuFileAsync",
            file,
            parserMock.Object,
            logger,
            CancellationToken.None);

        AssertHttpResultType(result, "Ok");
    }

    [Fact]
    public async Task UpdateMenuDateAsync_WhenMenuNotFound_ShouldReturnNotFound()
    {
        Mock<IMenuRepository> menuRepositoryMock = new();
        menuRepositoryMock.Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Menu?)null);

        object logger = CreateProgramLogger();
        DefaultHttpContext httpContext = CreateHttpContext("admin-user");

        IResult result = await InvokePrivateResultMethodAsync(
            "UpdateMenuDateAsync",
            Guid.NewGuid(),
            DateTime.Today.AddDays(5),
            menuRepositoryMock.Object,
            logger,
            httpContext,
            CancellationToken.None);

        AssertHttpResultType(result, "NotFound");
    }

    [Fact]
    public async Task UpdateMenuDateAsync_WhenDomainExceptionThrown_ShouldReturnBadRequest()
    {
        Menu menu = CreateValidMenu();

        Mock<IMenuRepository> menuRepositoryMock = new();
        menuRepositoryMock.Setup(repository => repository.GetByIdAsync(menu.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(menu);
        menuRepositoryMock.Setup(repository => repository.UpdateDateAsync(menu, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("Invalid date"));

        object logger = CreateProgramLogger();
        DefaultHttpContext httpContext = CreateHttpContext("admin-user");

        IResult result = await InvokePrivateResultMethodAsync(
            "UpdateMenuDateAsync",
            menu.Id,
            DateTime.Today,
            menuRepositoryMock.Object,
            logger,
            httpContext,
            CancellationToken.None);

        AssertHttpResultType(result, "BadRequest");
    }

    [Fact]
    public async Task UpdateMenuActiveUntilAsync_WhenMenuDeleted_ShouldReturnBadRequest()
    {
        Menu menu = CreateValidMenu();
        menu.SoftDeleteWithPendingOrders([]);

        Mock<IMenuRepository> menuRepositoryMock = new();
        menuRepositoryMock.Setup(repository => repository.GetByIdAsync(menu.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(menu);

        object logger = CreateProgramLogger();
        DefaultHttpContext httpContext = CreateHttpContext("admin-user");

        IResult result = await InvokePrivateResultMethodAsync(
            "UpdateMenuActiveUntilAsync",
            menu.Id,
            DateTime.Today.AddDays(1).AddHours(13),
            menuRepositoryMock.Object,
            logger,
            httpContext,
            CancellationToken.None);

        AssertHttpResultType(result, "BadRequest");
    }

    [Fact]
    public async Task SoftDeleteMenuAsync_WhenDomainExceptionThrown_ShouldReturnBadRequest()
    {
        Mock<IMenuRepository> menuRepositoryMock = new();
        menuRepositoryMock.Setup(repository => repository.SoftDeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("Cannot delete"));

        object logger = CreateProgramLogger();
        DefaultHttpContext httpContext = CreateHttpContext("admin-user");

        IResult result = await InvokePrivateResultMethodAsync(
            "SoftDeleteMenuAsync",
            Guid.NewGuid(),
            menuRepositoryMock.Object,
            logger,
            httpContext,
            CancellationToken.None);

        AssertHttpResultType(result, "BadRequest");
    }

    private static CreateMenuDto CreateValidCreateMenuDto()
    {
        Guid supplierId = Guid.NewGuid();
        return new CreateMenuDto(
            supplierId,
            DateTime.Today.AddDays(2),
            DateTime.Today.AddDays(1).AddHours(12),
            [new CreateMealDto("Soup", 10m)]);
    }

    private static Menu CreateValidMenu()
    {
        Guid supplierId = Guid.NewGuid();
        Meal seedMeal = Meal.Create(Guid.NewGuid(), "Soup", new Price(10m));
        return Menu.Create(
            supplierId,
            DateTime.Today.AddDays(2),
            DateTime.Today.AddDays(1).AddHours(12),
            [seedMeal]);
    }

    private static IFormFile CreateFile(string fileName, string content)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content);
        MemoryStream stream = new(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName);
    }

    private static async Task<IResult> InvokePrivateResultMethodAsync(string methodName, params object?[] parameters)
    {
        MethodInfo? methodInfo = typeof(MenuEndpoints)
            .GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(methodInfo);

        object? invocationResult = methodInfo.Invoke(null, parameters);
        Task<IResult> task = Assert.IsType<Task<IResult>>(invocationResult);
        return await task;
    }

    private static object CreateProgramLogger()
    {
        Type programType = typeof(MenuEndpoints).Assembly.GetType("Program")
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
