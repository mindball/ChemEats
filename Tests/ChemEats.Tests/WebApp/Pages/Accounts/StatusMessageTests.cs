using Bunit;
using WebApp.Pages.Accounts;

namespace ChemEats.Tests.WebApp.Pages.Accounts;

public class StatusMessageTests : TestContext
{
    [Fact]
    public void StatusMessage_WhenMessageIsNull_ShouldRenderNothing()
    {
        IRenderedComponent<StatusMessage> component = RenderComponent<StatusMessage>();

        Assert.DoesNotContain("alert", component.Markup);
    }

    [Fact]
    public void StatusMessage_WhenMessageStartsWithError_ShouldRenderDangerAlert()
    {
        IRenderedComponent<StatusMessage> component = RenderComponent<StatusMessage>(
            parameters => parameters.Add(parameter => parameter.Message, "Error: invalid credentials"));

        Assert.Contains("alert-danger", component.Markup);
        Assert.Contains("Error: invalid credentials", component.Markup);
    }

    [Fact]
    public void StatusMessage_WhenMessageDoesNotStartWithError_ShouldRenderSuccessAlert()
    {
        IRenderedComponent<StatusMessage> component = RenderComponent<StatusMessage>(
            parameters => parameters.Add(parameter => parameter.Message, "Login successful"));

        Assert.Contains("alert-success", component.Markup);
        Assert.Contains("Login successful", component.Markup);
    }
}
