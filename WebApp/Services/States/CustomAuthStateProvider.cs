// using System.Net.Http.Headers;
// using Microsoft.AspNetCore.Components.Authorization;
// using System.Net.Http.Json;
// using System.Security.Claims;
// using System.Text.Json.Nodes;
//
// namespace WebApp.Services.States
// {
//     public class CustomAuthStateProvider : AuthenticationStateProvider
//     {
//         private readonly HttpClient _httpClient;
//
//         public CustomAuthStateProvider(HttpClient httpClient)
//         {
//             _httpClient = httpClient;
//         }
//
//         public override async Task<AuthenticationState> GetAuthenticationStateAsync()
//         {
//             var user = new ClaimsPrincipal(new ClaimsIdentity());
//
//             try
//             {
//                 var response = await _httpClient.GetAsync("manage/info");
//                 if (response.IsSuccessStatusCode)
//                 {
//                     var strResponse = await response.Content.ReadAsStringAsync();
//                     var jsonRespone = JsonNode.Parse(strResponse);
//                     var email = jsonRespone!["email"]!.ToString();
//
//                     var claims = new List<Claim>
//                     {
//                         new(ClaimTypes.Name, email),
//                         new(ClaimTypes.Email, email)
//                     };
//
//                     var identity = new ClaimsIdentity(claims, "Token");
//                     user = new ClaimsPrincipal(identity);
//                     return new AuthenticationState(user);
//                 }
//             }
//             catch (Exception e)
//             {
//                 Console.WriteLine(e);
//                 throw;
//             }
//
//             return new AuthenticationState(user);
//         }
//
//         public async Task<FormResult> LoginAsync(string email, string password)
//         {
//             try
//             {
//                 var response = await _httpClient.PostAsJsonAsync("login", new { email, password });
//                 if (response.IsSuccessStatusCode)
//                 {
//                     var strResponse = await response.Content.ReadAsStringAsync();
//                     var jsonRespone = JsonNode.Parse(strResponse);
//                     var accessToken = jsonRespone?["accessToken"]?.ToString();
//                     var refreshToken = jsonRespone?["refreshToken"]?.ToString();
//
//                     _httpClient.DefaultRequestHeaders.Authorization =
//                         new AuthenticationHeaderValue("Bearer", accessToken);
//
//                     NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
//
//                     return new FormResult { Succeeded = true };
//                 }
//                 else
//                 {
//                     return new FormResult { Succeeded = false, Errors = ["Bad Email or Password"] };
//                 }
//             }
//             catch (Exception e)
//             {
//                 Console.WriteLine(e);
//                 throw;
//             }
//         }
//
//         public async Task LogoutAsync()
//         {
//             // Премахваме Authorization header
//             _httpClient.DefaultRequestHeaders.Authorization = null;
//
//             // Задаваме празен user
//             var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
//             NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymousUser)));
//
//             await Task.CompletedTask;
//         }
//     }
//
//     public class FormResult
//     {
//         public bool Succeeded { get; set; }
//         public string[] Errors { get; set; } = [];
//     }
// }

using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json.Nodes;

namespace WebApp.Services.States
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly HttpClient _httpClient;
        private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

        public CustomAuthStateProvider(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            return new AuthenticationState(_currentUser);
        }

        public async Task<FormResult> LoginAsync(string email, string password)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("login", new { email, password });
                if (!response.IsSuccessStatusCode)
                    return new FormResult { Succeeded = false, Errors = new[] { "Bad Email or Password" } };

                var strResponse = await response.Content.ReadAsStringAsync();
                var jsonRespone = JsonNode.Parse(strResponse);
                var accessToken = jsonRespone?["accessToken"]?.ToString();

                if (string.IsNullOrEmpty(accessToken))
                    return new FormResult { Succeeded = false, Errors = new[] { "No token received" } };

                // Добавяне на токена към HttpClient
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);

                // Създаване на ClaimsPrincipal
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, email),
                    new Claim(ClaimTypes.Email, email)
                };

                _currentUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));

                // Уведомяване на Blazor за промяната
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));

                return new FormResult { Succeeded = true };
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return new FormResult { Succeeded = false, Errors = new[] { e.Message } };
            }
        }

        public async Task LogoutAsync()
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
            await Task.CompletedTask;
        }
    }

    public class FormResult
    {
        public bool Succeeded { get; set; }
        public string[] Errors { get; set; } = Array.Empty<string>();
    }
}
