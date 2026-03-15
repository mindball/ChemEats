using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.DTOs.Employees;
using WebApi.Infrastructure.Employees;

namespace ChemEats.Tests.WebApi.Infrastructure.Employees;

public class EmployeeExternalServiceTests
{
    [Fact]
    public async Task GetAllEmployeesAsync_WhenResponseIsSuccessful_ShouldReturnEmployees()
    {
        List<UserDto> payload =
        [
            new UserDto { Code = "MM", Name = "Main Manager" },
            new UserDto { Code = "DM", Name = "Deputy Manager" }
        ];

        HttpClient httpClient = CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(payload)
        });

        Mock<ILogger<EmployeeExternalService>> loggerMock = new();
        EmployeeExternalService service = new(httpClient, loggerMock.Object);

        List<UserDto> result = await service.GetAllEmployeesAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("MM", result[0].Code);
    }

    [Fact]
    public async Task GetAllEmployeesAsync_WhenResponseIsFailure_ShouldReturnEmptyCollection()
    {
        HttpClient httpClient = CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        Mock<ILogger<EmployeeExternalService>> loggerMock = new();
        EmployeeExternalService service = new(httpClient, loggerMock.Object);

        List<UserDto> result = await service.GetAllEmployeesAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetEmployeeByAbbreviationAsync_WhenResponseIsSuccessful_ShouldReturnEmployee()
    {
        UserDto payload = new() { Code = "MM", Name = "Main Manager" };

        HttpClient httpClient = CreateHttpClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(payload)
        });

        Mock<ILogger<EmployeeExternalService>> loggerMock = new();
        EmployeeExternalService service = new(httpClient, loggerMock.Object);

        UserDto? result = await service.GetEmployeeByAbbreviationAsync("MM");

        Assert.NotNull(result);
        Assert.Equal("Main Manager", result.Name);
    }

    [Fact]
    public async Task GetEmployeeByAbbreviationAsync_WhenHttpThrows_ShouldReturnNull()
    {
        HttpClient httpClient = new(new ThrowingHttpMessageHandler())
        {
            BaseAddress = new Uri("https://localhost/")
        };

        Mock<ILogger<EmployeeExternalService>> loggerMock = new();
        EmployeeExternalService service = new(httpClient, loggerMock.Object);

        UserDto? result = await service.GetEmployeeByAbbreviationAsync("MM");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetEmployeeByAbbreviationAsync_ShouldUseExpectedQueryPath()
    {
        Uri? capturedUri = null;
        HttpClient httpClient = CreateHttpClient(request =>
        {
            capturedUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new UserDto { Code = "MM", Name = "Main Manager" })
            };
        });

        Mock<ILogger<EmployeeExternalService>> loggerMock = new();
        EmployeeExternalService service = new(httpClient, loggerMock.Object);

        UserDto? result = await service.GetEmployeeByAbbreviationAsync("MM");

        Assert.NotNull(result);
        Assert.NotNull(capturedUri);
        Assert.Equal("/Employee?Abbreviation=MM", capturedUri!.PathAndQuery);
    }

    private static HttpClient CreateHttpClient(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        HttpClient client = new(new StubHttpMessageHandler(handler))
        {
            BaseAddress = new Uri("https://localhost/")
        };

        return client;
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = _handler(request);
            return Task.FromResult(response);
        }
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Network failure", null, HttpStatusCode.BadGateway);
        }
    }
}
