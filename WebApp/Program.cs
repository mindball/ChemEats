using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Globalization;
using WebApp;
using WebApp.Services.Menus;
using WebApp.Services.Suppliers;
using WebApp.Services.States;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HttpClients
builder.Services.AddScoped<ISupplierDataService, SupplierDataService>();
builder.Services.AddScoped<IMenuDataService, MenuDataService>();

builder.Services.AddScoped((sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) }));



// Това е нужно за AuthorizeView
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
    provider.GetRequiredService<CustomAuthStateProvider>());

// Настройка на култура
CultureInfo bgCulture = new CultureInfo("bg-BG");
CultureInfo.DefaultThreadCurrentCulture = bgCulture;
CultureInfo.DefaultThreadCurrentUICulture = bgCulture;

await builder.Build().RunAsync();