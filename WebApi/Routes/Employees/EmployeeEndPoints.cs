using Domain.Infrastructure.Identity;
using Domain.Repositories.Employees;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Shared;
using Shared.DTOs.Employees;
using WebApi.Infrastructure.Employees;
using WebApi.Infrastructure.Filters;
using WebApi.Infrastructure.Identity;

namespace WebApi.Routes.Employees;

public static class EmployeeEndPoints
{
    public static void MapEmployeeEndpoints(this WebApplication app)
    {
        app.MapPost(ApiRoutes.Employees.SyncEmployees, SyncEmployeesAsync)
            .RequireAuthorization("AdminPolicy")
            .WithTags("System Maintenance")
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();

        app.MapGet(ApiRoutes.Employees.Base, GetAllEmployeesAsync)
            .RequireAuthorization("AdminPolicy")
            .WithTags("Employees")
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();

        app.MapPost(ApiRoutes.Employees.Base + "/" + ApiRoutes.Employees.RolesRoute, AssignRoleAsync)
            .RequireAuthorization("AdminPolicy")
            .WithTags("Employees")
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();

        app.MapDelete(ApiRoutes.Employees.Base + "/" + ApiRoutes.Employees.RolesRoute, RemoveRoleAsync)
            .RequireAuthorization("AdminPolicy")
            .WithTags("Employees")
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();

        app.MapPost(ApiRoutes.Employees.Base + "/" + ApiRoutes.Employees.MyPassword, ChangeMyPasswordAsync)
            .RequireAuthorization()
            .WithTags("Employees")
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();

        app.MapPost(ApiRoutes.Employees.Base + "/" + ApiRoutes.Employees.ResetPasswordRoute, ResetUserPasswordAsync)
            .RequireAuthorization("AdminPolicy")
            .WithTags("Employees")
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();

        app.MapPost(ApiRoutes.Employees.Login, LoginAsync);
    }

    private static async Task<IResult> SyncEmployeesAsync(
        IEmployeeSyncService syncService,
        ILogger<IEmployeeSyncService> logger)
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
    }

    private static async Task<IResult> GetAllEmployeesAsync(
        IUserRepository userRepository,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Retrieving all employees with roles");

            List<ApplicationUser> employees = await userRepository.GetAllUsersAsync(cancellationToken);

            List<object> dtos = [];
            foreach (ApplicationUser employee in employees)
            {
                IList<string> roles = await userRepository.GetRolesAsync(employee, cancellationToken);
                dtos.Add(new
                {
                    UserId = employee.Id,
                    FullName = employee.FullName,
                    Email = employee.Email,
                    Abbreviation = employee.Abbreviation,
                    Roles = roles
                });
            }

            logger.LogInformation("Retrieved {EmployeeCount} employees", dtos.Count);

            return Results.Ok(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving employees: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    private static async Task<IResult> AssignRoleAsync(
        string userId,
        string roleName,
        IUserRepository userRepository,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Assigning role {Role} to user {UserId}", roleName, userId);

        if (!Guid.TryParse(userId, out Guid userGuid))
            return Results.BadRequest("Invalid user ID.");

        ApplicationUser? user = await userRepository.GetByIdAsync(userGuid, cancellationToken);
        if (user is null)
            return Results.NotFound("User not found.");

        if (!await userRepository.RoleExistsAsync(roleName, cancellationToken))
            return Results.BadRequest($"Role '{roleName}' does not exist.");

        IList<string> currentRoles = await userRepository.GetRolesAsync(user, cancellationToken);
        if (currentRoles.Contains(roleName))
            return Results.Ok("User already has this role.");

        IdentityResult result = await userRepository.AddToRoleAsync(user, roleName, cancellationToken);
        if (!result.Succeeded)
        {
            string errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogError("Failed to assign role {Role} to user {UserId}: {Errors}", roleName, userId, errors);
            return Results.BadRequest(errors);
        }

        logger.LogInformation("Role {Role} assigned to user {UserId} successfully", roleName, userId);
        return Results.Ok("Role assigned.");
    }

    private static async Task<IResult> RemoveRoleAsync(
        string userId,
        string roleName,
        IUserRepository userRepository,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Removing role {Role} from user {UserId}", roleName, userId);

        if (!Guid.TryParse(userId, out Guid userGuid))
            return Results.BadRequest("Invalid user ID.");

        ApplicationUser? user = await userRepository.GetByIdAsync(userGuid, cancellationToken);
        if (user is null)
            return Results.NotFound("User not found.");

        IList<string> currentRoles = await userRepository.GetRolesAsync(user, cancellationToken);
        if (!currentRoles.Contains(roleName))
            return Results.Ok("User does not have this role.");

        IdentityResult result = await userRepository.RemoveFromRoleAsync(user, roleName, cancellationToken);
        if (!result.Succeeded)
        {
            string errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogError("Failed to remove role {Role} from user {UserId}: {Errors}", roleName, userId, errors);
            return Results.BadRequest(errors);
        }

        logger.LogInformation("Role {Role} removed from user {UserId} successfully", roleName, userId);
        return Results.Ok("Role removed.");
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest req,
        IUserRepository userRepo,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        JwtTokenProvider jwtProvider,
        ILogger<JwtTokenProvider> logger)
    {
        logger.LogInformation("Login attempt for identifier: {Identifier}", req.Email);

        try
        {
            ApplicationUser? user = await userRepo.FindByEmailAsync(req.Email)
                ?? await userRepo.FindByUserNameAsync(req.Email);

            if (user == null)
            {
                logger.LogWarning("Login failed - User not found: {Identifier}", req.Email);
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

            bool requiresPasswordChange = string.Equals(
                req.Password,
                user.Abbreviation,
                StringComparison.OrdinalIgnoreCase);

            return Results.Ok(new
            {
                AccessToken = accessToken,
                Username = user.UserName,
                Email = user.Email,
                Roles = roles,
                RequiresPasswordChange = requiresPasswordChange
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Login process failed for user: {Email} with error: {ErrorMessage}",
                req.Email, ex.Message);
            throw;
        }
    }

    private static async Task<IResult> ChangeMyPasswordAsync(
        ChangePasswordRequestDto request,
        UserManager<ApplicationUser> userManager,
        IUserRepository userRepository,
        HttpContext httpContext,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        ApplicationUser? user = await userManager.GetUserAsync(httpContext.User);
        if (user is null)
            return Results.Unauthorized();

        if (request.NewPassword != request.ConfirmPassword)
            return Results.BadRequest("Password confirmation does not match.");

        if (string.Equals(request.NewPassword, user.Abbreviation, StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest("New password cannot be the same as your abbreviation.");

        IdentityResult result = await userRepository.ChangePasswordAsync(
            user,
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);

        if (!result.Succeeded)
        {
            string errors = string.Join(", ", result.Errors.Select(error => error.Description));
            logger.LogWarning(
                "Password change failed for user {UserId}: {Errors}",
                user.Id,
                errors);
            return Results.BadRequest(errors);
        }

        logger.LogInformation("Password changed successfully for user {UserId}", user.Id);
        return Results.Ok("Password changed successfully.");
    }

    private static async Task<IResult> ResetUserPasswordAsync(
        string userId,
        IUserRepository userRepository,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(userId, out Guid userGuid))
            return Results.BadRequest("Invalid user ID.");

        ApplicationUser? user = await userRepository.GetByIdAsync(userGuid, cancellationToken);
        if (user is null)
            return Results.NotFound("User not found.");

        IdentityResult result = await userRepository.ResetPasswordAsync(user, user.Abbreviation, cancellationToken);

        if (!result.Succeeded)
        {
            string errors = string.Join(", ", result.Errors.Select(error => error.Description));
            logger.LogWarning("Password reset failed for user {UserId}: {Errors}", user.Id, errors);
            return Results.BadRequest(errors);
        }

        logger.LogInformation("Password reset successfully for user {UserId}", user.Id);
        return Results.Ok("User password reset to abbreviation.");
    }
}
