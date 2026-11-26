using WebApp.Resources;
using ChemEats.Tests.TestInfrastructure;
using AngleSharp.Dom;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shared.DTOs.Obligations;
using WebApp.Pages.AdminPanel;
using WebApp.Services.Obligations;

namespace ChemEats.Tests.WebApp.Pages.AdminPanel;

public class AdminObligationTypesPageTests : LocalizedTestContext
{
    private readonly Mock<IObligationTypeDataService> _typeDataServiceMock = new();

    public AdminObligationTypesPageTests()
    {
        Services.AddSingleton(_typeDataServiceMock.Object);
    }

    [Fact]
    public void AdminObligationTypes_WhenTypesLoaded_ShouldRenderOneRowPerType()
    {
        _typeDataServiceMock.Setup(service => service.GetObligationTypesAsync())
            .ReturnsAsync([new ObligationTypeDto(Guid.NewGuid(), "Keyboard"), new ObligationTypeDto(Guid.NewGuid(), "Printer toner")]);

        IRenderedComponent<AdminObligationTypes> component = RenderComponent<AdminObligationTypes>();

        component.WaitForAssertion(() =>
        {
            IRefreshableElementCollection<IElement> rows = component.FindAll("tbody tr");
            Assert.Equal(2, rows.Count);
            Assert.Contains("Keyboard", rows[0].TextContent);
            Assert.Contains("Printer toner", rows[1].TextContent);
        });
    }

    [Fact]
    public void AddButton_WhenNameIsEntered_ShouldCreateTypeAndReload()
    {
        _typeDataServiceMock.SetupSequence(service => service.GetObligationTypesAsync())
            .ReturnsAsync([])
            .ReturnsAsync([new ObligationTypeDto(Guid.NewGuid(), "Printer toner")]);
        _typeDataServiceMock.Setup(service => service.CreateObligationTypeAsync("Printer toner"))
            .ReturnsAsync(new ObligationTypeDto(Guid.NewGuid(), "Printer toner"));

        IRenderedComponent<AdminObligationTypes> component = RenderComponent<AdminObligationTypes>();

        component.Find("#newObligationTypeName").Change("  Printer toner ");
        component.Find("button.add-obligation-type-button").Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Expense type \"Printer toner\" added.", component.Find(".alert-success").TextContent);
            Assert.Single(component.FindAll("tbody tr"));
        });
        _typeDataServiceMock.Verify(service => service.CreateObligationTypeAsync("Printer toner"), Times.Once);
    }

    [Fact]
    public void AddButton_WhenNameIsEmpty_ShouldShowErrorAndNotCallService()
    {
        _typeDataServiceMock.Setup(service => service.GetObligationTypesAsync()).ReturnsAsync([]);

        IRenderedComponent<AdminObligationTypes> component = RenderComponent<AdminObligationTypes>();

        component.Find("button.add-obligation-type-button").Click();

        Assert.Contains("Name is required.", component.Find(".alert-danger").TextContent);
        _typeDataServiceMock.Verify(service => service.CreateObligationTypeAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void AddButton_WhenServiceRejectsDuplicate_ShouldShowError()
    {
        _typeDataServiceMock.Setup(service => service.GetObligationTypesAsync()).ReturnsAsync([]);
        _typeDataServiceMock.Setup(service => service.CreateObligationTypeAsync("Toner"))
            .ReturnsAsync((ObligationTypeDto?)null);

        IRenderedComponent<AdminObligationTypes> component = RenderComponent<AdminObligationTypes>();

        component.Find("#newObligationTypeName").Change("Toner");
        component.Find("button.add-obligation-type-button").Click();

        component.WaitForAssertion(() =>
            Assert.Contains("The name may already exist.", component.Find(".alert-danger").TextContent));
    }

    [Fact]
    public void DeleteButton_WhenConfirmed_ShouldDeleteType()
    {
        Guid typeId = Guid.NewGuid();
        _typeDataServiceMock.Setup(service => service.GetObligationTypesAsync())
            .ReturnsAsync([new ObligationTypeDto(typeId, "Toner")]);
        _typeDataServiceMock.Setup(service => service.DeleteObligationTypeAsync(typeId)).ReturnsAsync(true);
        JSInterop.Setup<bool>("confirm", _ => true).SetResult(true);

        IRenderedComponent<AdminObligationTypes> component = RenderComponent<AdminObligationTypes>();

        component.WaitForAssertion(() => component.Find("button[title=Delete]").Click());

        component.WaitForAssertion(() =>
            _typeDataServiceMock.Verify(service => service.DeleteObligationTypeAsync(typeId), Times.Once));
    }
}
