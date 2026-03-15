using Bunit;
using AngleSharp.Dom;
using WebApp.Components;

namespace ChemEats.Tests.WebApp.Components;

public class PopupModalTests : TestContext
{
    [Fact]
    public void PopupModal_ShouldNotRenderModalBody_WhenNotVisible()
    {
        IRenderedComponent<PopupModal> component = RenderComponent<PopupModal>();

        Assert.DoesNotContain("modal fade show d-block", component.Markup);
    }

    [Fact]
    public void PopupModal_Show_ShouldRenderTitleAndMessage_AndCloseShouldHide()
    {
        IRenderedComponent<PopupModal> component = RenderComponent<PopupModal>();

        component.InvokeAsync(() => component.Instance.Show("Warning", "Demo message"));

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Warning", component.Markup);
            Assert.Contains("Demo message", component.Markup);
        });

        IElement closeButton = component.Find("button.btn-close");
        closeButton.Click();

        component.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Warning", component.Markup);
            Assert.DoesNotContain("Demo message", component.Markup);
        });
    }
}
