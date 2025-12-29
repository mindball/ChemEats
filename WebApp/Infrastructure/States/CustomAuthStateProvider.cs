using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using WebApp.Infrastructure.Helpers;

namespace WebApp.Infrastructure.States;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _httpClient;
    private readonly SessionStorageService _sessionStorage;

    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    public CustomAuthStateProvider(HttpClient httpClient, SessionStorageService sessionStorage)
    {
        _httpClient = httpClient;
        _sessionStorage = sessionStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        string? savedToken = await _sessionStorage.GetItemAsync<string>("authToken");

        if (string.IsNullOrWhiteSpace(savedToken))
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", savedToken);

        IEnumerable<Claim> claims = JwtClaimParser.ParseClaims(savedToken).ToList();
        ClaimsIdentity identity = new(claims, "jwt");
        ClaimsPrincipal user = new(identity);

        _currentUser = user;

        return new AuthenticationState(user);
    }

    public async Task<FormResult> LoginAsync(string email, string password)
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/login", new { email, password });
        if (!response.IsSuccessStatusCode)
            return new FormResult { Succeeded = false, Errors = ["Bad Email or Password"] };

        string json = await response.Content.ReadAsStringAsync();
        JsonDocument doc = JsonDocument.Parse(json);
        string? token = doc.RootElement.GetProperty("accessToken").GetString();

        if (string.IsNullOrWhiteSpace(token))
            return new FormResult { Succeeded = false, Errors = ["No token received"] };

        await _sessionStorage.SetItemAsync("authToken", token);

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        List<Claim> claims = JwtClaimParser.ParseClaims(token).ToList();
        ClaimsIdentity identity = new(claims, "jwt");
        _currentUser = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));

        return new FormResult { Succeeded = true };
    }

    public async Task LogoutAsync()
    {
        await _sessionStorage.RemoveItemAsync("authToken");
        _httpClient.DefaultRequestHeaders.Authorization = null;

        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());

        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(_currentUser)));
    }   

    public class FormResult
    {
        public bool Succeeded { get; set; }
        public string[] Errors { get; set; } = Array.Empty<string>();
    }
}