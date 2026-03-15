using ChemEats.Tests.TestInfrastructure;
using Moq;
using Shared.Common.Enums;
using Shared.DTOs.Meals;
using Shared.DTOs.Suppliers;
using WebApp.Pages.Menus;
using WebApp.Services.Menus;
using WebApp.Services.Suppliers;

namespace ChemEats.Tests.WebApp.Pages.Menus;

public class UploadMenuBaseTests
{
    [Fact]
    public async Task LoadSuppliersAsync_ShouldPopulateSuppliers_WhenServiceReturnsData()
    {
        Mock<IMenuDataService> menuServiceMock = new();
        Mock<ISupplierDataService> supplierServiceMock = new();

        supplierServiceMock
            .Setup(service => service.GetAllSuppliersAsync())
            .ReturnsAsync([new SupplierDto { Id = Guid.NewGuid(), Name = "Supplier One", VatNumber = "BG123", PaymentTerms = PaymentTermsUI.Net10, Menus = [] }]);

        UploadMenuBaseTestHarness harness = CreateHarness(menuServiceMock.Object, supplierServiceMock.Object);

        await harness.LoadSuppliersAsyncPublic();

        Assert.Single(harness.SuppliersPublic);
        Assert.Null(harness.ErrorMessagePublic);
        Assert.False(harness.IsLoadingPublic);
    }

    [Fact]
    public async Task LoadSuppliersAsync_ShouldSetErrorMessage_WhenServiceThrows()
    {
        Mock<IMenuDataService> menuServiceMock = new();
        Mock<ISupplierDataService> supplierServiceMock = new();

        supplierServiceMock
            .Setup(service => service.GetAllSuppliersAsync())
            .ThrowsAsync(new InvalidOperationException("failed"));

        UploadMenuBaseTestHarness harness = CreateHarness(menuServiceMock.Object, supplierServiceMock.Object);

        await harness.LoadSuppliersAsyncPublic();

        Assert.Contains("Failed to load suppliers", harness.ErrorMessagePublic);
        Assert.False(harness.IsLoadingPublic);
    }

    [Fact]
    public async Task HandleUploadAsync_ShouldRequireSupplierSelection()
    {
        Mock<IMenuDataService> menuServiceMock = new();
        Mock<ISupplierDataService> supplierServiceMock = new();

        UploadMenuBaseTestHarness harness = CreateHarness(menuServiceMock.Object, supplierServiceMock.Object);
        harness.SetParsedMeals([new CreateMealDto("Soup", 10m)]);

        await harness.HandleUploadAsyncPublic();

        Assert.Equal("Please select a supplier.", harness.ErrorMessagePublic);
    }

    [Fact]
    public async Task HandleUploadAsync_ShouldRequireMeals()
    {
        Mock<IMenuDataService> menuServiceMock = new();
        Mock<ISupplierDataService> supplierServiceMock = new();

        UploadMenuBaseTestHarness harness = CreateHarness(menuServiceMock.Object, supplierServiceMock.Object);
        harness.SetSelectedSupplierId(Guid.NewGuid());

        await harness.HandleUploadAsyncPublic();

        Assert.Equal("Please upload a file with meals.", harness.ErrorMessagePublic);
    }

    [Fact]
    public async Task HandleUploadAsync_ShouldValidateMealNameAndPrice()
    {
        Mock<IMenuDataService> menuServiceMock = new();
        Mock<ISupplierDataService> supplierServiceMock = new();

        UploadMenuBaseTestHarness harness = CreateHarness(menuServiceMock.Object, supplierServiceMock.Object);
        harness.SetSelectedSupplierId(Guid.NewGuid());
        harness.SetParsedMeals([new CreateMealDto(" ", 10m)]);

        await harness.HandleUploadAsyncPublic();

        Assert.Equal("All meals must have a name.", harness.ErrorMessagePublic);

        harness.SetParsedMeals([new CreateMealDto("Soup", 0m)]);

        await harness.HandleUploadAsyncPublic();

        Assert.Equal("All meals must have a price greater than 0.", harness.ErrorMessagePublic);
    }

    private static UploadMenuBaseTestHarness CreateHarness(IMenuDataService menuService, ISupplierDataService supplierService)
    {
        UploadMenuBaseTestHarness harness = new();
        harness.SetDependencies(menuService, supplierService, new TestNavigationManager());
        return harness;
    }

    private sealed class UploadMenuBaseTestHarness : UploadMenuBase
    {
        public string? ErrorMessagePublic => ErrorMessage;
        public bool IsLoadingPublic => IsLoading;
        public List<SupplierDto> SuppliersPublic => Suppliers;

        public Task HandleUploadAsyncPublic() => HandleUploadAsync();

        public Task LoadSuppliersAsyncPublic() => LoadSuppliersAsync();

        public void SetDependencies(IMenuDataService menuService, ISupplierDataService supplierService, Microsoft.AspNetCore.Components.NavigationManager navigationManager)
        {
            MenuService = menuService;
            SupplierService = supplierService;
            Navigation = navigationManager;
        }

        public void SetParsedMeals(List<CreateMealDto> meals)
        {
            ParsedMeals = meals;
        }

        public void SetSelectedSupplierId(Guid supplierId)
        {
            SelectedSupplierId = supplierId;
        }
    }
}
