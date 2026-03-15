using System.Security.Claims;
using Domain.Common.Enums;
using Domain.Entities;
using Domain.Infrastructure.Identity;
using Domain.Repositories.Menus;
using Domain.Repositories.Suppliers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shared.DTOs.Meals;
using Shared.DTOs.Menus;
using WebApi.Infrastructure.Filters;

namespace ChemEats.Tests.WebApi.Infrastructure.Filters;

public class SupplierSupervisorFilterTests
{
    [Fact]
    public async Task InvokeAsync_WhenUserIsAdmin_ShouldCallNext()
    {
        SupplierSupervisorFilter filter = new();
        DefaultHttpContext httpContext = CreateHttpContext(CreatePrincipal("admin-id", isAdmin: true));
        TestEndpointFilterInvocationContext context = new(httpContext);

        bool nextCalled = false;
        EndpointFilterDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        };

        object? result = await filter.InvokeAsync(context, next);

        Assert.True(nextCalled);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task InvokeAsync_WhenSupplierIsResolvedFromMenuIdAndUserIsSupervisor_ShouldCallNext()
    {
        Guid supplierId = Guid.NewGuid();
        Guid menuId = Guid.NewGuid();

        Supplier supplier = new(supplierId, "Supplier", "BG123", PaymentTerms.Net10);
        supplier.AssignSupervisor("employee-id");

        Meal meal = Meal.Create(Guid.NewGuid(), "Soup", new Price(8m));
        Menu menu = Menu.Create(supplierId, DateTime.Today.AddDays(3), DateTime.Today.AddDays(1).AddHours(12), [meal]);

        SupplierSupervisorFilter filter = new();

        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(new ApplicationUser { Id = "employee-id", UserName = "employee" });

        Mock<IMenuRepository> menuRepositoryMock = new();
        menuRepositoryMock.Setup(repository => repository.GetByIdAsync(menuId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(menu);

        Mock<ISupplierRepository> supplierRepositoryMock = new();
        supplierRepositoryMock.Setup(repository => repository.GetByIdAsync(supplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(supplier);

        DefaultHttpContext httpContext = CreateHttpContext(CreatePrincipal("employee-id"));
        httpContext.Request.RouteValues["menuId"] = menuId.ToString();
        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton(userManagerMock.Object)
            .AddSingleton(menuRepositoryMock.Object)
            .AddSingleton(supplierRepositoryMock.Object)
            .BuildServiceProvider();

        TestEndpointFilterInvocationContext context = new(httpContext);

        bool nextCalled = false;
        EndpointFilterDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        };

        object? result = await filter.InvokeAsync(context, next);

        Assert.True(nextCalled);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task InvokeAsync_WhenMenuIdDoesNotResolveSupplier_ShouldReturnForbid()
    {
        Guid menuId = Guid.NewGuid();

        SupplierSupervisorFilter filter = new();
        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(new ApplicationUser { Id = "employee-id", UserName = "employee" });

        Mock<IMenuRepository> menuRepositoryMock = new();
        menuRepositoryMock.Setup(repository => repository.GetByIdAsync(menuId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Menu?)null);

        DefaultHttpContext httpContext = CreateHttpContext(CreatePrincipal("employee-id"));
        httpContext.Request.RouteValues["menuId"] = menuId.ToString();
        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton(userManagerMock.Object)
            .AddSingleton(menuRepositoryMock.Object)
            .BuildServiceProvider();

        TestEndpointFilterInvocationContext context = new(httpContext);
        EndpointFilterDelegate next = _ => ValueTask.FromResult<object?>(Results.Ok());

        object? result = await filter.InvokeAsync(context, next);

        AssertHttpResultType(result, "ForbidHttpResult");
    }

    [Fact]
    public async Task InvokeAsync_WhenUserManagerReturnsNull_ShouldReturnUnauthorized()
    {
        SupplierSupervisorFilter filter = new();
        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((ApplicationUser?)null);

        DefaultHttpContext httpContext = CreateHttpContext(CreatePrincipal("employee-id"));
        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton(userManagerMock.Object)
            .BuildServiceProvider();

        TestEndpointFilterInvocationContext context = new(httpContext);
        EndpointFilterDelegate next = _ => ValueTask.FromResult<object?>(Results.Ok());

        object? result = await filter.InvokeAsync(context, next);

        AssertHttpResultType(result, "UnauthorizedHttpResult");
    }

    [Fact]
    public async Task InvokeAsync_WhenSupplierCannotBeResolved_ShouldReturnForbid()
    {
        SupplierSupervisorFilter filter = new();
        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(new ApplicationUser { Id = "employee-id", UserName = "employee" });

        DefaultHttpContext httpContext = CreateHttpContext(CreatePrincipal("employee-id"));
        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton(userManagerMock.Object)
            .BuildServiceProvider();

        TestEndpointFilterInvocationContext context = new(httpContext);
        EndpointFilterDelegate next = _ => ValueTask.FromResult<object?>(Results.Ok());

        object? result = await filter.InvokeAsync(context, next);

        AssertHttpResultType(result, "ForbidHttpResult");
    }

    [Fact]
    public async Task InvokeAsync_WhenSupplierIsNotFound_ShouldReturnNotFound()
    {
        Guid supplierId = Guid.NewGuid();
        SupplierSupervisorFilter filter = new();

        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(new ApplicationUser { Id = "employee-id", UserName = "employee" });

        Mock<ISupplierRepository> supplierRepositoryMock = new();
        supplierRepositoryMock.Setup(repository => repository.GetByIdAsync(supplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Supplier?)null);

        DefaultHttpContext httpContext = CreateHttpContext(CreatePrincipal("employee-id"));
        httpContext.Request.RouteValues["supplierId"] = supplierId.ToString();
        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton(userManagerMock.Object)
            .AddSingleton(supplierRepositoryMock.Object)
            .BuildServiceProvider();

        TestEndpointFilterInvocationContext context = new(httpContext);
        EndpointFilterDelegate next = _ => ValueTask.FromResult<object?>(Results.Ok());

        object? result = await filter.InvokeAsync(context, next);

        AssertHttpResultType(result, "NotFound");
    }

    [Fact]
    public async Task InvokeAsync_WhenUserIsNotSupervisor_ShouldReturnForbid()
    {
        Guid supplierId = Guid.NewGuid();
        Supplier supplier = new(supplierId, "Supplier", "BG123", PaymentTerms.Net10);
        supplier.AssignSupervisor("other-user");

        SupplierSupervisorFilter filter = new();

        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(new ApplicationUser { Id = "employee-id", UserName = "employee" });

        Mock<ISupplierRepository> supplierRepositoryMock = new();
        supplierRepositoryMock.Setup(repository => repository.GetByIdAsync(supplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(supplier);

        DefaultHttpContext httpContext = CreateHttpContext(CreatePrincipal("employee-id"));
        httpContext.Request.RouteValues["supplierId"] = supplierId.ToString();
        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton(userManagerMock.Object)
            .AddSingleton(supplierRepositoryMock.Object)
            .BuildServiceProvider();

        TestEndpointFilterInvocationContext context = new(httpContext);
        EndpointFilterDelegate next = _ => ValueTask.FromResult<object?>(Results.Ok());

        object? result = await filter.InvokeAsync(context, next);

        AssertHttpResultType(result, "ForbidHttpResult");
    }

    [Fact]
    public async Task InvokeAsync_WhenUserIsSupervisorFromCreateMenuDto_ShouldCallNext()
    {
        Guid supplierId = Guid.NewGuid();
        Supplier supplier = new(supplierId, "Supplier", "BG123", PaymentTerms.Net10);
        supplier.AssignSupervisor("employee-id");

        SupplierSupervisorFilter filter = new();

        Mock<UserManager<ApplicationUser>> userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(new ApplicationUser { Id = "employee-id", UserName = "employee" });

        Mock<ISupplierRepository> supplierRepositoryMock = new();
        supplierRepositoryMock.Setup(repository => repository.GetByIdAsync(supplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(supplier);

        CreateMenuDto createMenuDto = new(supplierId, DateTime.Today.AddDays(2), DateTime.Today.AddDays(1).AddHours(12), [new CreateMealDto("Soup", 7m)]);

        DefaultHttpContext httpContext = CreateHttpContext(CreatePrincipal("employee-id"));
        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton(userManagerMock.Object)
            .AddSingleton(supplierRepositoryMock.Object)
            .BuildServiceProvider();

        TestEndpointFilterInvocationContext context = new(httpContext, createMenuDto);

        bool nextCalled = false;
        EndpointFilterDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        };

        object? result = await filter.InvokeAsync(context, next);

        Assert.True(nextCalled);
        Assert.NotNull(result);
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

    private static DefaultHttpContext CreateHttpContext(ClaimsPrincipal principal)
    {
        DefaultHttpContext httpContext = new();
        httpContext.User = principal;
        return httpContext;
    }

    private static ClaimsPrincipal CreatePrincipal(string userId, bool isAdmin = false)
    {
        List<Claim> claims =
        [
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userId)
        ];

        if (isAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }

        ClaimsIdentity identity = new(claims, "TestAuth", ClaimTypes.Name, ClaimTypes.Role);
        return new ClaimsPrincipal(identity);
    }

    private static void AssertHttpResultType(object? result, string expectedTypeName)
    {
        Assert.NotNull(result);
        IResult httpResult = Assert.IsAssignableFrom<IResult>(result);
        Assert.Contains(expectedTypeName, httpResult.GetType().Name, StringComparison.Ordinal);
    }

    private sealed class TestEndpointFilterInvocationContext : EndpointFilterInvocationContext
    {
        private readonly object?[] _arguments;

        public TestEndpointFilterInvocationContext(HttpContext httpContext, params object?[] arguments)
        {
            HttpContext = httpContext;
            _arguments = arguments;
        }

        public override HttpContext HttpContext { get; }

        public override IList<object?> Arguments => _arguments;

        public override T GetArgument<T>(int index)
        {
            return (T)_arguments[index]!;
        }
    }
}
