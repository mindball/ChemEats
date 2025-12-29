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
        app.MapPost("/api/sync-employees", async (IEmployeeSyncService syncService) =>
            {
                await syncService.SyncEmployeesAsync();
                return Results.Ok("Employees synchronized successfully.");
            })
            .RequireAuthorization("AdminPolicy") 
            .WithTags("System Maintenance")
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();

        app.MapPost("/api/login", async (
            LoginRequest req,
            IUserRepository userRepo,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            JwtTokenProvider jwtProvider) => 
        {
            var user = await userRepo.FindByEmailAsync(req.Email);
            if (user == null)
                return Results.Unauthorized();

            var result = await signInManager.CheckPasswordSignInAsync(
                user,
                req.Password,
                lockoutOnFailure: false);

            if (!result.Succeeded)
                return Results.Unauthorized();

            var roles = await userManager.GetRolesAsync(user);

            var accessToken = jwtProvider.GenerateToken(user, roles);

            return Results.Ok(new
            {
                AccessToken = accessToken,
                Username = user.UserName,
                Email = user.Email,
                Roles = roles
            });
        });

    }
}