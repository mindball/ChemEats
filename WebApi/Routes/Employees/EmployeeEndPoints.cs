using Domain.Infrastructure.Identity;
using Domain.Repositories.Employees;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using WebApi.Infrastructure.Employees;
using WebApi.Infrastructure.Filters;
using WebApi.Infrastructure.Identity;

namespace WebApi.Routes.Employees;

public static class EmployeeEndPoints
{
    public static void MapEmployeeEndpoints(this WebApplication app)
    {
        app.MapPost("/api/sync-employees", async (
            IEmployeeSyncService syncService,
            ILogger<IEmployeeSyncService> logger) =>
            {
                try
                {
                        logger.LogInformation("Starting employee synchronization process");
                    System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                    
                    await syncService.SyncEmployeesAsync();
                    
                    sw.Stop();
                    logger.LogInformation("Employee synchronization completed successfully in {ElapsedMs} ms", sw.ElapsedMilliseconds);
                    
                    return Results.Ok("Employees synchronized successfully.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Employee synchronization failed with error: {ErrorMessage}", ex.Message);
                    throw;
                }
            })
            .RequireAuthorization("AdminPolicy") 
            .WithTags("System Maintenance")
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();

        app.MapGet("/api/employees", async (
                IUserRepository userRepository,
                ILogger<Program> logger,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    logger.LogInformation("Retrieving all employees for admin dropdown");

                    List<ApplicationUser> employees = await userRepository.GetAllUsersAsync(cancellationToken);

                    var dtos = employees.Select(e => new
                    {
                        UserId = e.Id,
                        FullName = e.FullName,
                        Email = e.Email,
                        Abbreviation = e.Abbreviation
                    }).ToList();

                    logger.LogInformation("Retrieved {EmployeeCount} employees", dtos.Count);

                    return Results.Ok(dtos);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error retrieving employees: {ErrorMessage}", ex.Message);
                    throw;
                }
            })
            .RequireAuthorization("AdminPolicy")
            .WithTags("Employees")
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();

        app.MapPost("/api/login", async (
            LoginRequest req,
            IUserRepository userRepo,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            JwtTokenProvider jwtProvider,
            ILogger<JwtTokenProvider> logger) => 
        {
            logger.LogInformation("Login attempt for user: {Email}", req.Email);
            
            try
            {
                ApplicationUser? user = await userRepo.FindByEmailAsync(req.Email);
                if (user == null)
                {
                    logger.LogWarning("Login failed - User not found: {Email}", req.Email);
                    return Results.Unauthorized();
                }

                logger.LogInformation("User found: {Abbreviation}, {UserName}. Checking password", user.Abbreviation, user.UserName);

                SignInResult result = await signInManager.CheckPasswordSignInAsync(
                    user,
                    req.Password,
                    lockoutOnFailure: false);

                if (!result.Succeeded)
                {
                    logger.LogWarning("Login failed - Invalid password for user: {Email}, UserId: {Abbreviation}", 
                        req.Email, user.Abbreviation);
                    return Results.Unauthorized();
                }

                IList<string> roles = await userManager.GetRolesAsync(user);
                logger.LogInformation("Password validated successfully. User {UserName} ({Abbreviation}) has roles: {Roles}", 
                    user.UserName, user.Abbreviation, string.Join(", ", roles));

                string accessToken = jwtProvider.GenerateToken(user, roles);
                
                logger.LogInformation("JWT token generated successfully for user: {UserName} ({Abbreviation})", 
                    user.UserName, user.Abbreviation);

                return Results.Ok(new
                {
                    AccessToken = accessToken,
                    Username = user.UserName,
                    Email = user.Email,
                    Roles = roles
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Login process failed for user: {Email} with error: {ErrorMessage}", 
                    req.Email, ex.Message);
                throw;
            }
        });

    }
}