using Domain;
using Domain.Infrastructure.Identity;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApi.Extensions.DI;
using WebApi.Infrastructure;
using WebApi.Routes.Menus;
using WebApi.Routes.Suppliers;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Open",
        corsPolicyBuilder => corsPolicyBuilder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// builder.Services.AddRazorComponents()
//     .AddInteractiveServerComponents()
//     .AddInteractiveWebAssemblyComponents()
//     .AddAuthenticationStateSerialization();
//
//
// builder.Services.AddAuthentication(options =>
//     {
//         options.DefaultScheme = IdentityConstants.ApplicationScheme;
//         options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
//     })
//     .AddIdentityCookies();

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseFirebird(connectionString, providerOptions =>
        {
            providerOptions
                .WithExplicitStringLiteralTypes(false)
                .WithExplicitParameterTypes(false)
                .MigrationsAssembly("WebApi");
        })
        .UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
}, ServiceLifetime.Transient, ServiceLifetime.Singleton);

builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddEntityFrameworkStores<AppDbContext>();

// builder.Services
//     .AddIdentityCore<ApplicationUser>(options => { options.SignIn.RequireConfirmedAccount = false; })
//     .AddRoles<IdentityRole>()
//     .AddEntityFrameworkStores<AppDbContext>()
//     .AddSignInManager()
//     .AddDefaultTokenProviders();

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.RegisterRepositories();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var config = TypeAdapterConfig.GlobalSettings;
config.Scan(typeof(SupplierMappingConfig).Assembly);
builder.Services.AddSingleton(config);
builder.Services.AddScoped<IMapper, ServiceMapper>();


WebApplication app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ChemEats API v1");
        options.RoutePrefix = "swagger";
    });
    app.UseWebAssemblyDebugging();
}


app.MapIdentityApi<ApplicationUser>();
app.MapSwagger().RequireAuthorization();

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapMenuEndpoints();
app.MapSupplierEndpoints();
app.UseAntiforgery();

app.UseCors("Open");

app.MapFallbackToFile("index.html");

// using (IServiceScope scope = app.Services.CreateScope())
// {
//     IServiceProvider services = scope.ServiceProvider;
//     UserManager<ApplicationUser> userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
//     RoleManager<IdentityRole> roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
//
//     await IdentitySeeder.SeedAsync(userManager, roleManager);
// }

app.Run();