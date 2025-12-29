using Domain;
using Domain.Infrastructure.Identity;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WebApi.Extensions.DI;
using WebApi.Infrastructure.Employees;
using WebApi.Infrastructure.Identity;

namespace WebApi.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        return services;
    }

    public static IServiceCollection AddAppCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("Open", cors =>
                cors.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
        });
        return services;
    }

    public static IServiceCollection AddAppDb(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<AppDbContext>(options =>
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

        return services;
    }

    public static IServiceCollection AddAppIdentity(this IServiceCollection services)
    {
        services.AddIdentityApiEndpoints<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 1;
            options.Password.RequiredUniqueChars = 0;
            options.User.RequireUniqueEmail = false;
            options.SignIn.RequireConfirmedEmail = false;
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
        });

        return services;
    }

    public static IServiceCollection AddAppJwt(this IServiceCollection services, IConfiguration configuration)
    {
        // Jwt settings provider used for issuing tokens
        services.AddSingleton<JwtTokenProvider>(sp =>
        {
            IConfiguration config = sp.GetRequiredService<IConfiguration>();
            JwtSettings jwtSettings = new(
                config["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer not configured"),
                config["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience not configured"),
                config["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret not configured"),
                int.TryParse(config["Jwt:ExpiryMinutes"], out int expiry) ? expiry : 60
            );
            return new JwtTokenProvider(jwtSettings);
        });

        // Jwt bearer validation for incoming requests
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            string issuer = configuration["Jwt:Issuer"]!;
            string audience = configuration["Jwt:Audience"]!;
            string secret = configuration["Jwt:Secret"]!;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });

        return services;
    }

    public static IServiceCollection AddAppMapster(this IServiceCollection services)
    {
        TypeAdapterConfig mapsterConfig = TypeAdapterConfig.GlobalSettings;
        mapsterConfig.Scan(typeof(SupplierMappingConfig).Assembly);
        services.AddSingleton(mapsterConfig);
        services.AddScoped<IMapper, ServiceMapper>();
        return services;
    }

    public static IServiceCollection AddAppEmployees(this IServiceCollection services, IConfiguration configuration)
    {
        IConfigurationSection employeeAddress = configuration.GetSection("EmployeesAddress");
        services.AddHttpClient<IEmployeeExternalService, EmployeeExternalService>(client =>
        {
            if (employeeAddress.Value != null)
                client.BaseAddress = new Uri(employeeAddress.Value);
        });
        services.AddMemoryCache();
        services.AddScoped<IEmployeeCacheService, EmployeeCacheService>();
        services.AddScoped<IEmployeeSyncService, EmployeeSyncService>();
        return services;
    }

    public static IServiceCollection AddAppEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.AddTransient<IEmailSender<ApplicationUser>, SmtpEmailSender>();
        return services;
    }

    public static IServiceCollection AddAppRazorComponents(this IServiceCollection services)
    {
        services.AddRazorComponents().AddInteractiveWebAssemblyComponents();
        return services;
    }

    public static IServiceCollection AddAppRepositories(this IServiceCollection services)
    {
        services.RegisterRepositories();
        return services;
    }
}