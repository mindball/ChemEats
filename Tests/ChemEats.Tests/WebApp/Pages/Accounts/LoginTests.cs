using System.Net;
using System.Net.Http;
using System.Text;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Infrastructure.States;
using WebApp.Pages.Accounts;

namespace ChemEats.Tests.WebApp.Pages.Accounts;

public class LoginTests : TestContext
{
    [Fact]
    public void Login_WhenCredentialsAreInvalid_ShouldRenderErrorMessage()
    {
        HttpClient httpClient = CreateHttpClient(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        ConfigureLoginDependencies(httpClient);

        IRenderedComponent<Login> component = RenderComponent<Login>();

        component.Find("input#Input\\.UserName").Change("wrong-user");
        component.Find("input#Input\\.Password").Change("wrong-pass");
        component.Find("form").Submit();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Bad Email or Password", component.Markup);
        });
    }

    [Fact]
    public void Login_WhenCredentialsAreValid_ShouldNavigateToRoot()
    {
        string token = CreateJwtToken("user-1", "mm", "mm@cpachem.com");
        string responseJson = $"{{\"accessToken\":\"{token}\"}}";

        HttpResponseMessage successResponse = new(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        HttpClient httpClient = CreateHttpClient(successResponse);
        ConfigureLoginDependencies(httpClient);

        NavigationManager navigationManager = Services.GetRequiredService<NavigationManager>();

        IRenderedComponent<Login> component = RenderComponent<Login>();
        component.Find("input#Input\\.UserName").Change("mm");
        component.Find("input#Input\\.Password").Change("mm");
        component.Find("form").Submit();

        component.WaitForAssertion(() =>
        {
            Assert.EndsWith("/", navigationManager.Uri, StringComparison.Ordinal);
        });
    }

    private void ConfigureLoginDependencies(HttpClient httpClient)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddSingleton(httpClient);
        Services.AddScoped<SessionStorageService>();
        Services.AddScoped<CustomAuthStateProvider>();
        Services.AddScoped<AuthenticationStateProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<CustomAuthStateProvider>());
    }

    private static HttpClient CreateHttpClient(HttpResponseMessage response)
    {
        HttpClient client = new(new StaticResponseMessageHandler(response))
        {
            BaseAddress = new Uri("http://localhost/")
        };

        return client;
    }

    private static string CreateJwtToken(string userId, string userName, string email)
    {
        string headerJson = "{\"alg\":\"none\",\"typ\":\"JWT\"}";
        string payloadJson = $"{{\"sub\":\"{userId}\",\"name\":\"{userName}\",\"email\":\"{email}\"}}";

        string header = Base64UrlEncode(headerJson);
        string payload = Base64UrlEncode(payloadJson);

        return $"{header}.{payload}.signature";
    }

    private static string Base64UrlEncode(string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        string base64 = Convert.ToBase64String(bytes);

        return base64
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private sealed class StaticResponseMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public StaticResponseMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }
    }
}
