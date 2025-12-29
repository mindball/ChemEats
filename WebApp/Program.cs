using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Globalization;
using WebApp;
using WebApp.Infrastructure.States;
using WebApp.Services.Menus;
using WebApp.Services.Orders;
using WebApp.Services.Suppliers;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HttpClients
builder.Services.AddScoped<ISupplierDataService, SupplierDataService>();
builder.Services.AddScoped<IMenuDataService, MenuDataService>();
builder.Services.AddScoped<IOrderDataService, OrderDataService>();

builder.Services.AddScoped((sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) }));

// Това е нужно за AuthorizeView
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<SessionStorageService>();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
    provider.GetRequiredService<CustomAuthStateProvider>());

// Настройка на култура
var bgCulture = new CultureInfo("bg-BG");
var bgEuro = (CultureInfo)bgCulture.Clone();
bgEuro.NumberFormat = (NumberFormatInfo)bgCulture.NumberFormat.Clone();
bgEuro.NumberFormat.CurrencySymbol = "€";

CultureInfo.DefaultThreadCurrentCulture = bgEuro;
CultureInfo.DefaultThreadCurrentUICulture = bgEuro;

await builder.Build().RunAsync();