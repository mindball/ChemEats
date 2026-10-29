using Moq;
using Shared.DTOs.Notifications;
using WebApp.Pages.Accounts;
using WebApp.Services.Notifications;

namespace ChemEats.Tests.WebApp.Pages.Accounts;

public class NotificationSettingsBaseTests
{
    [Fact]
    public async Task OnInitializedAsync_ShouldLoadCurrentSettings()
    {
        Mock<INotificationDataService> serviceMock = new();
        serviceMock.Setup(service => service.GetMySettingsAsync())
            .ReturnsAsync(new NotificationSettingsDto("personal@example.com", true));

        NotificationSettingsBaseTestHarness harness = CreateHarness(serviceMock.Object);

        await harness.OnInitializedAsyncPublic();

        Assert.Equal("personal@example.com", harness.ModelPublic.Email);
        Assert.True(harness.ModelPublic.RemindersEnabled);
        Assert.Null(harness.ErrorMessagePublic);
        Assert.False(harness.IsLoadingPublic);
    }

    [Fact]
    public async Task OnInitializedAsync_WhenServiceReturnsNull_ShouldSetErrorMessage()
    {
        Mock<INotificationDataService> serviceMock = new();
        serviceMock.Setup(service => service.GetMySettingsAsync())
            .ReturnsAsync((NotificationSettingsDto?)null);

        NotificationSettingsBaseTestHarness harness = CreateHarness(serviceMock.Object);

        await harness.OnInitializedAsyncPublic();

        Assert.Equal("Could not load your notification settings.", harness.ErrorMessagePublic);
    }

    [Fact]
    public async Task SaveAsync_WhenRemindersEnabledWithoutEmail_ShouldSetErrorAndNotCallService()
    {
        Mock<INotificationDataService> serviceMock = new();
        NotificationSettingsBaseTestHarness harness = CreateHarness(serviceMock.Object);
        harness.ModelPublic.Email = "  ";
        harness.ModelPublic.RemindersEnabled = true;

        await harness.SaveAsyncPublic();

        Assert.Equal("Enter an email address to receive order reminders.", harness.ErrorMessagePublic);
        serviceMock.Verify(service => service.UpdateMySettingsAsync(It.IsAny<NotificationSettingsDto>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_WhenEmailIsInvalid_ShouldSetErrorAndNotCallService()
    {
        Mock<INotificationDataService> serviceMock = new();
        NotificationSettingsBaseTestHarness harness = CreateHarness(serviceMock.Object);
        harness.ModelPublic.Email = "not-an-email";
        harness.ModelPublic.RemindersEnabled = true;

        await harness.SaveAsyncPublic();

        Assert.Equal("Please enter a valid email address.", harness.ErrorMessagePublic);
        serviceMock.Verify(service => service.UpdateMySettingsAsync(It.IsAny<NotificationSettingsDto>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_WhenValid_ShouldSendTrimmedSettingsAndShowSuccess()
    {
        NotificationSettingsDto? sent = null;
        Mock<INotificationDataService> serviceMock = new();
        serviceMock.Setup(service => service.UpdateMySettingsAsync(It.IsAny<NotificationSettingsDto>()))
            .Callback<NotificationSettingsDto>(dto => sent = dto)
            .ReturnsAsync((NotificationSettingsDto dto) => dto);

        NotificationSettingsBaseTestHarness harness = CreateHarness(serviceMock.Object);
        harness.ModelPublic.Email = " personal@example.com ";
        harness.ModelPublic.RemindersEnabled = true;

        await harness.SaveAsyncPublic();

        Assert.Equal(new NotificationSettingsDto("personal@example.com", true), sent);
        Assert.Equal("Notification settings saved.", harness.SuccessMessagePublic);
        Assert.Null(harness.ErrorMessagePublic);
        Assert.False(harness.IsSavingPublic);
    }

    [Fact]
    public async Task SaveAsync_WhenDisablingWithEmptyEmail_ShouldSendNullEmail()
    {
        NotificationSettingsDto? sent = null;
        Mock<INotificationDataService> serviceMock = new();
        serviceMock.Setup(service => service.UpdateMySettingsAsync(It.IsAny<NotificationSettingsDto>()))
            .Callback<NotificationSettingsDto>(dto => sent = dto)
            .ReturnsAsync((NotificationSettingsDto dto) => dto);

        NotificationSettingsBaseTestHarness harness = CreateHarness(serviceMock.Object);
        harness.ModelPublic.Email = string.Empty;
        harness.ModelPublic.RemindersEnabled = false;

        await harness.SaveAsyncPublic();

        Assert.Equal(new NotificationSettingsDto(null, false), sent);
    }

    [Fact]
    public async Task SaveAsync_WhenServiceReturnsNull_ShouldSetErrorMessage()
    {
        Mock<INotificationDataService> serviceMock = new();
        serviceMock.Setup(service => service.UpdateMySettingsAsync(It.IsAny<NotificationSettingsDto>()))
            .ReturnsAsync((NotificationSettingsDto?)null);

        NotificationSettingsBaseTestHarness harness = CreateHarness(serviceMock.Object);
        harness.ModelPublic.Email = "personal@example.com";
        harness.ModelPublic.RemindersEnabled = true;

        await harness.SaveAsyncPublic();

        Assert.Equal("Saving failed. Please check the email address and try again.", harness.ErrorMessagePublic);
        Assert.Null(harness.SuccessMessagePublic);
        Assert.False(harness.IsSavingPublic);
    }

    private static NotificationSettingsBaseTestHarness CreateHarness(INotificationDataService service)
    {
        NotificationSettingsBaseTestHarness harness = new();
        harness.SetDependencies(service);
        return harness;
    }

    private sealed class NotificationSettingsBaseTestHarness : NotificationSettingsBase
    {
        public NotificationSettingsModel ModelPublic => Model;
        public string? ErrorMessagePublic => ErrorMessage;
        public string? SuccessMessagePublic => SuccessMessage;
        public bool IsLoadingPublic => IsLoading;
        public bool IsSavingPublic => IsSaving;

        public Task OnInitializedAsyncPublic() => OnInitializedAsync();

        public Task SaveAsyncPublic() => SaveAsync();

        public void SetDependencies(INotificationDataService service)
        {
            NotificationDataService = service;
        }
    }
}
