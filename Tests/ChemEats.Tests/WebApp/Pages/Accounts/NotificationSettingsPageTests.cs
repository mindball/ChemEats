using WebApp.Resources;
using ChemEats.Tests.TestInfrastructure;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shared.DTOs.Notifications;
using WebApp.Pages.Accounts;
using WebApp.Services.Notifications;

namespace ChemEats.Tests.WebApp.Pages.Accounts;

public class NotificationSettingsPageTests : LocalizedTestContext
{
    [Fact]
    public void NotificationSettings_WhenAuthorized_ShouldRenderLoadedSettings()
    {
        TestAuthorizationContext authorizationContext = this.AddTestAuthorization();
        authorizationContext.SetAuthorized("MM");

        Mock<INotificationDataService> serviceMock = new();
        serviceMock.Setup(service => service.GetMySettingsAsync())
            .ReturnsAsync(new NotificationSettingsDto("personal@example.com", true));
        Services.AddSingleton(serviceMock.Object);

        IRenderedComponent<NotificationSettings> component = RenderComponent<NotificationSettings>();

        Assert.Contains("Notification Settings", component.Markup);
        Assert.Equal("personal@example.com", component.Find("#notification-email").GetAttribute("value"));
        Assert.True(component.Find("#reminders-on").HasAttribute("checked"));
        Assert.False(component.Find("#reminders-off").HasAttribute("checked"));
    }

    [Fact]
    public void NotificationSettings_WhenSubmittingValidSettings_ShouldSaveAndShowSuccess()
    {
        TestAuthorizationContext authorizationContext = this.AddTestAuthorization();
        authorizationContext.SetAuthorized("MM");

        Mock<INotificationDataService> serviceMock = new();
        serviceMock.Setup(service => service.GetMySettingsAsync())
            .ReturnsAsync(new NotificationSettingsDto(null, false));
        serviceMock.Setup(service => service.UpdateMySettingsAsync(It.IsAny<NotificationSettingsDto>()))
            .ReturnsAsync((NotificationSettingsDto dto) => dto);
        Services.AddSingleton(serviceMock.Object);

        IRenderedComponent<NotificationSettings> component = RenderComponent<NotificationSettings>();

        component.Find("#notification-email").Change("personal@example.com");
        // Browsers report a radio's value attribute as the change value.
        AngleSharp.Dom.IElement remindersOn = component.Find("#reminders-on");
        remindersOn.Change(remindersOn.GetAttribute("value"));
        component.Find("form").Submit();

        component.WaitForAssertion(() => Assert.Contains("Notification settings saved.", component.Markup));
        serviceMock.Verify(service => service.UpdateMySettingsAsync(new NotificationSettingsDto("personal@example.com", true)), Times.Once);
    }

    [Fact]
    public void NotificationSettings_WhenLoadFails_ShouldShowError()
    {
        TestAuthorizationContext authorizationContext = this.AddTestAuthorization();
        authorizationContext.SetAuthorized("MM");

        Mock<INotificationDataService> serviceMock = new();
        serviceMock.Setup(service => service.GetMySettingsAsync())
            .ReturnsAsync((NotificationSettingsDto?)null);
        Services.AddSingleton(serviceMock.Object);

        IRenderedComponent<NotificationSettings> component = RenderComponent<NotificationSettings>();

        Assert.Contains("Could not load your notification settings.", component.Markup);
    }

    [Fact]
    public void NotificationSettings_WhenNotAuthorized_ShouldRedirectToLogin()
    {
        TestAuthorizationContext authorizationContext = this.AddTestAuthorization();
        authorizationContext.SetNotAuthorized();

        Mock<INotificationDataService> serviceMock = new();
        Services.AddSingleton(serviceMock.Object);

        NavigationManager navigationManager = Services.GetRequiredService<NavigationManager>();

        RenderComponent<NotificationSettings>();

        Assert.Contains("Account/Login", navigationManager.Uri, StringComparison.OrdinalIgnoreCase);
    }
}
