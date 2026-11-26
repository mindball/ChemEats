using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Globalization;
using WebApp;
using WebApp.Infrastructure.Localization;
using WebApp.Infrastructure.States;
using WebApp.Services.Employees;
using WebApp.Services.Menus;
using WebApp.Services.Notifications;
using WebApp.Services.Obligations;
using WebApp.Services.Orders;
using WebApp.Services.Settings;
using WebApp.Services.Suppliers;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");


// HttpClients
builder.Services.AddScoped<ISupplierDataService, SupplierDataService>();
builder.Services.AddScoped<IMenuDataService, MenuDataService>();
builder.Services.AddScoped<IOrderDataService, OrderDataService>();
builder.Services.AddScoped<ISettingsDataService, SettingsDataService>();
builder.Services.AddScoped<IEmployeeDataService, EmployeeDataService>();
builder.Services.AddScoped<IMenuReportService, MenuReportService>();
builder.Services.AddScoped<INotificationDataService, NotificationDataService>();
builder.Services.AddScoped<IObligationDataService, ObligationDataService>();
builder.Services.AddScoped<IObligationTypeDataService, ObligationTypeDataService>();


builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
    Timeout = TimeSpan.FromMinutes(6)
});

// Това е нужно за AuthorizeView
builder.Services.AddLocalization();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<SessionStorageService>();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
    provider.GetRequiredService<CustomAuthStateProvider>());

// Formatting is always bg-BG with EUR; the UI language (resx) comes from the user choice
WebAssemblyHost host = builder.Build();
await AppCulture.ApplyAsync(host.Services.GetRequiredService<Microsoft.JSInterop.IJSRuntime>());

await host.RunAsync();