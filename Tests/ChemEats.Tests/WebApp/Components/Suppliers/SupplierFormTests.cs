using Bunit;
using AngleSharp.Dom;
using Microsoft.AspNetCore.Components;
using Shared.Common.Enums;
using Shared.DTOs.Employees;
using Shared.DTOs.Suppliers;
using WebApp.Components.Suppliers;

namespace ChemEats.Tests.WebApp.Components.Suppliers;

public class SupplierFormTests : TestContext
{
    [Fact]
    public void SupplierForm_ShouldRenderSupplierFieldsAndSupervisorOptions()
    {
        UpdateSupplierDto supplier = new()
        {
            Name = "Supplier One",
            VatNumber = "BG123",
            PaymentTerms = PaymentTermsUI.Net10
        };

        List<EmployeeDto> employees =
        [
            new EmployeeDto("u1", "User One", "u1@cpachem.com", "U1"),
            new EmployeeDto("u2", "User Two", "u2@cpachem.com", "U2")
        ];

        IRenderedComponent<SupplierForm> component = RenderComponent<SupplierForm>(parameters => parameters
            .Add(parameter => parameter.Supplier, supplier)
            .Add(parameter => parameter.AvailableUsers, employees));

        Assert.Contains("Supplier One", component.Markup);
        Assert.Contains("BG123", component.Markup);
        Assert.Contains("User One (u1@cpachem.com)", component.Markup);
        Assert.Contains("User Two (u2@cpachem.com)", component.Markup);
    }

    [Fact]
    public void SupplierForm_WhenIsSubmittingIsTrue_ShouldRenderSubmitButtonTextAndBeDisabled()
    {
        UpdateSupplierDto supplier = new() { Name = "Supplier One", VatNumber = "BG123" };

        IRenderedComponent<SupplierForm> component = RenderComponent<SupplierForm>(parameters => parameters
            .Add(parameter => parameter.Supplier, supplier)
            .Add(parameter => parameter.IsSubmitting, true)
            .Add(parameter => parameter.SubmitButtonText, "Updating...")
            .Add(parameter => parameter.DefaultButtonText, "Update Supplier"));

        IElement submitButton = component.Find("button[type='submit']");

        Assert.Contains("Updating...", submitButton.TextContent);
        Assert.True(submitButton.HasAttribute("disabled"));
    }

    [Fact]
    public void SupplierForm_WhenFormIsSubmitted_ShouldInvokeOnValidSubmitCallback()
    {
        UpdateSupplierDto supplier = new() { Name = "Supplier One", VatNumber = "BG123" };
        bool callbackInvoked = false;

        IRenderedComponent<SupplierForm> component = RenderComponent<SupplierForm>(parameters => parameters
            .Add(parameter => parameter.Supplier, supplier)
            .Add(parameter => parameter.OnValidSubmit, EventCallback.Factory.Create(this, () => callbackInvoked = true)));

        component.Find("form").Submit();

        Assert.True(callbackInvoked);
    }

    [Fact]
    public void SupplierForm_ShouldRenderSuccessAndErrorMessages_WhenProvided()
    {
        UpdateSupplierDto supplier = new() { Name = "Supplier One", VatNumber = "BG123" };

        IRenderedComponent<SupplierForm> component = RenderComponent<SupplierForm>(parameters => parameters
            .Add(parameter => parameter.Supplier, supplier)
            .Add(parameter => parameter.SuccessMessage, "Saved successfully")
            .Add(parameter => parameter.ErrorMessage, "Validation failed"));

        Assert.Contains("Saved successfully", component.Markup);
        Assert.Contains("Validation failed", component.Markup);
    }
}
