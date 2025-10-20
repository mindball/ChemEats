using Domain;
using Domain.Infrastructure.Identity;
using Domain.Repositories.Employees;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using WebApi.Extensions.DI;
using WebApi.Infrastructure;
using WebApi.Infrastructure.Employees;
using WebApi.Routes.Emails;
using WebApi.Routes.Employees;
using WebApi.Routes.Menus;
using WebApi.Routes.Suppliers;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddOpenApi();


builder.Services.AddCors(options =>
{
    options.AddPolicy("Open",
        corsPolicyBuilder => corsPolicyBuilder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

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

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddTransient<IEmailSender<ApplicationUser>, SmtpEmailSender>();

builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
});

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 1;
    options.Password.RequiredUniqueChars = 0;

    // Допълнителни опции (по желание)
    options.User.RequireUniqueEmail = false;
    options.SignIn.RequireConfirmedEmail = false;
});

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.RegisterRepositories();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

TypeAdapterConfig config = TypeAdapterConfig.GlobalSettings;
config.Scan(typeof(SupplierMappingConfig).Assembly);
builder.Services.AddSingleton(config);
builder.Services.AddScoped<IMapper, ServiceMapper>();

builder.Services.AddHttpClient<IEmployeeExternalService, EmployeeExternalService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:8733/Everest/Employees/");
});
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IEmployeeCacheService, EmployeeCacheService>();
builder.Services.AddScoped<IEmployeeSyncService, EmployeeSyncService>();

WebApplication app = builder.Build();

// app.Use(async (context, next) =>
// {
//     ILogger<Program> logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
//     PathString path = context.Request.Path;
//     string method = context.Request.Method;
//     string? user = context.User.Identity?.IsAuthenticated == true
//         ? context.User.Identity.Name
//         : "Anonymous";
//
//     logger.LogInformation("Entering {Method} {Path} by {User}", method, path, user);
//
//     Stopwatch sw = Stopwatch.StartNew();
//     await next();
//     sw.Stop();
//
//     logger.LogInformation("Exiting {Method} {Path} by {User} with status {StatusCode} in {ElapsedMs} ms",
//         method, path, user, context.Response.StatusCode, sw.ElapsedMilliseconds);
// });

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
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.MapSwagger().RequireAuthorization(); 
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();
// app.UseRequireIdentityAuthorization();

app.UseCors("Open");
app.UseAuthentication();
app.UseAuthorization();

app
    .MapIdentityApi<ApplicationUser>();
    


app.MapMenuEndpoints();
app.MapSupplierEndpoints();
app.MapEmployeeEndpoints();
app.EmailEndpoints();

// using (IServiceScope scope = app.Services.CreateScope())
// {
//     var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
//     IEmployeeExternalService externalService = scope.ServiceProvider.GetRequiredService<IEmployeeExternalService>();
//
//     //Delete after deployed into production
//     // await IdentitySeeder.SeedAsync(userRepository, userManager, roleManager, externalService);
//
//     IEmployeeCacheService employeeCache = scope.ServiceProvider.GetRequiredService<IEmployeeCacheService>();
//     await employeeCache.InitializeAsync();
//     IEmployeeSyncService syncService = scope.ServiceProvider.GetRequiredService<IEmployeeSyncService>();
//     await syncService.SyncEmployeesAsync();
// }

app.UseAntiforgery();
app.MapFallbackToFile("index.html");

app.Run();
