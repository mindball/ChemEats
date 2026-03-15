using ChemEats.Tests.TestInfrastructure;
using Microsoft.JSInterop;
using Moq;
using Shared.Common.Enums;
using Shared.DTOs.Suppliers;
using WebApp.Pages.Suppliers;
using WebApp.Services.Suppliers;

namespace ChemEats.Tests.WebApp.Pages.Suppliers;

public class GetSuppliersBaseTests
{
    [Fact]
    public async Task OnInitializedAsync_ShouldLoadSuppliers()
    {
        SupplierDto supplier = CreateSupplier("Supplier One");

        Mock<ISupplierDataService> supplierServiceMock = new();
        supplierServiceMock.Setup(service => service.GetAllSuppliersAsync())
            .ReturnsAsync([supplier]);

        Mock<IJSRuntime> jsRuntimeMock = CreateJsRuntimeMock(confirmResult: true);
        TestNavigationManager navigationManager = new();

        GetSuppliersBaseTestHarness harness = CreateHarness(supplierServiceMock.Object, navigationManager, jsRuntimeMock.Object);

        await harness.OnInitializedAsyncPublic();

        Assert.Single(harness.SuppliersPublic);
        Assert.Equal("Supplier One", harness.SuppliersPublic[0].Name);
    }

    [Fact]
    public void ViewSupplier_AndCloseViewModal_ShouldManageSelectedSupplierState()
    {
        Mock<ISupplierDataService> supplierServiceMock = new();
        Mock<IJSRuntime> jsRuntimeMock = CreateJsRuntimeMock(confirmResult: true);
        TestNavigationManager navigationManager = new();

        GetSuppliersBaseTestHarness harness = CreateHarness(supplierServiceMock.Object, navigationManager, jsRuntimeMock.Object);

        SupplierDto supplier = CreateSupplier("Supplier One");

        harness.ViewSupplierPublic(supplier);

        Assert.True(harness.IsViewModalOpenPublic);
        Assert.Equal(supplier.Id, harness.SelectedSupplierPublic?.Id);

        harness.CloseViewModalPublic();

        Assert.False(harness.IsViewModalOpenPublic);
        Assert.Null(harness.SelectedSupplierPublic);
    }

    [Fact]
    public void UpdateSupplier_ShouldNavigateToEditRoute()
    {
        Mock<ISupplierDataService> supplierServiceMock = new();
        Mock<IJSRuntime> jsRuntimeMock = CreateJsRuntimeMock(confirmResult: true);
        TestNavigationManager navigationManager = new();

        GetSuppliersBaseTestHarness harness = CreateHarness(supplierServiceMock.Object, navigationManager, jsRuntimeMock.Object);
        SupplierDto supplier = CreateSupplier("Supplier One");

        harness.UpdateSupplierPublic(supplier);

        Assert.Contains($"edit-supplier/{supplier.Id}", navigationManager.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteSupplierAsync_WhenUserCancels_ShouldNotDeleteSupplier()
    {
        SupplierDto supplier = CreateSupplier("Supplier One");

        Mock<ISupplierDataService> supplierServiceMock = new();
        Mock<IJSRuntime> jsRuntimeMock = CreateJsRuntimeMock(confirmResult: false);
        TestNavigationManager navigationManager = new();

        GetSuppliersBaseTestHarness harness = CreateHarness(supplierServiceMock.Object, navigationManager, jsRuntimeMock.Object);
        harness.SuppliersPublic.Add(supplier);

        await harness.DeleteSupplierAsyncPublic(supplier);

        Assert.Single(harness.SuppliersPublic);
        supplierServiceMock.Verify(service => service.DeleteSupplierAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteSupplierAsync_WhenConfirmed_ShouldDeleteAndRemoveFromList()
    {
        SupplierDto supplier = CreateSupplier("Supplier One");

        Mock<ISupplierDataService> supplierServiceMock = new();
        supplierServiceMock.Setup(service => service.DeleteSupplierAsync(supplier.Id))
            .Returns(Task.CompletedTask);

        Mock<IJSRuntime> jsRuntimeMock = CreateJsRuntimeMock(confirmResult: true);
        TestNavigationManager navigationManager = new();

        GetSuppliersBaseTestHarness harness = CreateHarness(supplierServiceMock.Object, navigationManager, jsRuntimeMock.Object);
        harness.SuppliersPublic.Add(supplier);

        await harness.DeleteSupplierAsyncPublic(supplier);

        Assert.Empty(harness.SuppliersPublic);
        supplierServiceMock.Verify(service => service.DeleteSupplierAsync(supplier.Id), Times.Once);
    }

    private static SupplierDto CreateSupplier(string name)
    {
        return new SupplierDto
        {
            Id = Guid.NewGuid(),
            Name = name,
            VatNumber = "BG123",
            PaymentTerms = PaymentTermsUI.Net10,
            Menus = []
        };
    }

    private static GetSuppliersBaseTestHarness CreateHarness(ISupplierDataService supplierService, TestNavigationManager navigationManager, IJSRuntime jsRuntime)
    {
        GetSuppliersBaseTestHarness harness = new();
        harness.SetDependencies(supplierService, navigationManager, jsRuntime);

        return harness;
    }

    private static Mock<IJSRuntime> CreateJsRuntimeMock(bool confirmResult)
    {
        Mock<IJSRuntime> jsRuntimeMock = new();
        jsRuntimeMock
            .Setup(jsRuntime => jsRuntime.InvokeAsync<bool>("confirm", It.IsAny<object?[]>()))
            .ReturnsAsync(confirmResult);

        return jsRuntimeMock;
    }

    private sealed class GetSuppliersBaseTestHarness : GetSuppliersBase
    {
        public bool IsViewModalOpenPublic => IsViewModalOpen;
        public SupplierDto? SelectedSupplierPublic => SelectedSupplier;
        public List<SupplierDto> SuppliersPublic => Suppliers;

        public Task OnInitializedAsyncPublic() => OnInitializedAsync();

        public void ViewSupplierPublic(SupplierDto supplier) => ViewSupplier(supplier);

        public void CloseViewModalPublic() => CloseViewModal();

        public void UpdateSupplierPublic(SupplierDto supplier) => UpdateSupplier(supplier);

        public Task DeleteSupplierAsyncPublic(SupplierDto supplier) => DeleteSupplierAsync(supplier);

        public void SetDependencies(
            ISupplierDataService supplierService,
            Microsoft.AspNetCore.Components.NavigationManager navigationManager,
            IJSRuntime jsRuntime)
        {
            SupplierService = supplierService;
            NavigationManager = navigationManager;
            JSRuntime = jsRuntime;
        }
    }
}
