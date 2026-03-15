using Microsoft.JSInterop;
using Moq;
using Shared.Common.Enums;
using Shared.DTOs.Meals;
using Shared.DTOs.Menus;
using Shared.DTOs.Suppliers;
using System.Text;
using System.Text.Json;
using WebApp.Infrastructure.States;
using WebApp.Pages.Menus;
using WebApp.Services.Menus;
using WebApp.Services.Suppliers;

namespace ChemEats.Tests.WebApp.Pages.Menus;

public class ViewMenusBaseTests
{
    [Fact]
    public async Task OnInitializedAsync_ShouldSetAdminRoleAndLoadSuppliers()
    {
        Mock<IMenuDataService> menuDataServiceMock = new();
        Mock<IMenuReportService> reportServiceMock = new();
        Mock<ISupplierDataService> supplierDataServiceMock = new();
        supplierDataServiceMock
            .Setup(service => service.GetAllSuppliersAsync())
            .ReturnsAsync([new SupplierDto { Id = Guid.NewGuid(), Name = "Supplier One", VatNumber = "BG123", PaymentTerms = PaymentTermsUI.Net10, Menus = [] }]);

        CustomAuthStateProvider authProvider = CreateAuthStateProviderWithToken(CreateJwtTokenWithRole("Admin"));

        ViewMenusBaseTestHarness harness = CreateHarness(menuDataServiceMock.Object, reportServiceMock.Object, supplierDataServiceMock.Object, authProvider);

        await harness.OnInitializedAsyncPublic();

        Assert.True(harness.IsAdminPublic);
        Assert.True(harness.CanManageMenusPublic);
        Assert.Single(harness.SuppliersPublic);
    }

    [Fact]
    public async Task OnSearchClickedAsync_ShouldFilterAndPaginateMenus()
    {
        Guid supplierId = Guid.NewGuid();
        List<MenuDto> menus =
        [
            CreateMenu(supplierId, DateTime.Today.AddDays(1), "Supplier A"),
            CreateMenu(supplierId, DateTime.Today.AddDays(2), "Supplier A"),
            CreateMenu(Guid.NewGuid(), DateTime.Today.AddDays(3), "Supplier B")
        ];

        Mock<IMenuDataService> menuDataServiceMock = new();
        menuDataServiceMock
            .Setup(service => service.GetAllMenusAsync(false))
            .ReturnsAsync(menus);

        Mock<IMenuReportService> reportServiceMock = new();
        Mock<ISupplierDataService> supplierDataServiceMock = new();
        supplierDataServiceMock.Setup(service => service.GetAllSuppliersAsync()).ReturnsAsync([]);

        CustomAuthStateProvider authProvider = CreateAuthStateProviderWithToken(CreateJwtTokenWithRole("Supervisor"));

        ViewMenusBaseTestHarness harness = CreateHarness(menuDataServiceMock.Object, reportServiceMock.Object, supplierDataServiceMock.Object, authProvider);

        await harness.OnInitializedAsyncPublic();
        harness.SetFilters(supplierId, DateTime.Today, DateTime.Today.AddDays(2), false);

        await harness.OnSearchClickedAsyncPublic();

        Assert.NotNull(harness.MenusPublic);
        Assert.Equal(2, harness.TotalMenusPublic);
        Assert.All(harness.MenusPublic!, menu => Assert.Equal(supplierId, menu.SupplierId));
    }

    private static ViewMenusBaseTestHarness CreateHarness(
        IMenuDataService menuDataService,
        IMenuReportService menuReportService,
        ISupplierDataService supplierDataService,
        CustomAuthStateProvider authProvider)
    {
        Mock<IJSRuntime> jsRuntimeMock = new();
        jsRuntimeMock.Setup(jsRuntime => jsRuntime.InvokeAsync<string>("prompt", It.IsAny<object?[]>()))!.ReturnsAsync((string?)null);
        jsRuntimeMock.Setup(jsRuntime => jsRuntime.InvokeAsync<bool>("confirm", It.IsAny<object?[]>())).ReturnsAsync(false);

        ViewMenusBaseTestHarness harness = new(
            menuDataService,
            menuReportService,
            supplierDataService,
            authProvider,
            jsRuntimeMock.Object);
        return harness;
    }

    private static MenuDto CreateMenu(Guid supplierId, DateTime date, string supplierName)
    {
        return new MenuDto(
            Guid.NewGuid(),
            supplierId,
            supplierName,
            date,
            DateTime.Today.AddDays(1).AddHours(12),
            true,
            false,
            [new MealDto(Guid.NewGuid(), Guid.NewGuid(), "Soup", 10m)]);
    }

    private static CustomAuthStateProvider CreateAuthStateProviderWithToken(string token)
    {
        HttpClient httpClient = new() { BaseAddress = new Uri("http://localhost/") };
        Mock<IJSRuntime> jsRuntimeMock = new();
        jsRuntimeMock.Setup(jsRuntime => jsRuntime.InvokeAsync<string>("sessionStorage.getItem", It.IsAny<object?[]>()))
            .ReturnsAsync(JsonSerializer.Serialize(token));

        SessionStorageService sessionStorageService = new(jsRuntimeMock.Object);
        return new CustomAuthStateProvider(httpClient, sessionStorageService);
    }

    private static string CreateJwtTokenWithRole(string role)
    {
        string header = Base64UrlEncode("{\"alg\":\"none\",\"typ\":\"JWT\"}");
        string payload = Base64UrlEncode($"{{\"sub\":\"user-1\",\"name\":\"user\",\"role\":\"{role}\"}}");

        return $"{header}.{payload}.signature";
    }

    private static string Base64UrlEncode(string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        string base64 = Convert.ToBase64String(bytes);
        return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private sealed class ViewMenusBaseTestHarness : ViewMenusBase
    {
        public ViewMenusBaseTestHarness(
            IMenuDataService menuDataService,
            IMenuReportService menuReportService,
            ISupplierDataService supplierDataService,
            CustomAuthStateProvider authProvider,
            IJSRuntime jsRuntime)
        {
            MenuDataService = menuDataService;
            MenuReportService = menuReportService;
            SupplierDataService = supplierDataService;
            AuthProvider = authProvider;
            JsRuntime = jsRuntime;
        }

        public bool CanManageMenusPublic => CanManageMenus;
        public bool IsAdminPublic => IsAdmin;
        public IReadOnlyList<MenuDto>? MenusPublic => Menus;
        public List<SupplierDto> SuppliersPublic => Suppliers;
        public int TotalMenusPublic => TotalMenus;

        public Task OnInitializedAsyncPublic() => OnInitializedAsync();

        public Task OnSearchClickedAsyncPublic() => OnSearchClickedAsync();

        public void SetFilters(Guid supplierId, DateTime startDate, DateTime endDate, bool includeDeleted)
        {
            SelectedSupplierId = supplierId;
            StartDate = startDate;
            EndDate = endDate;
            IncludeDeleted = includeDeleted;
        }
    }
}
