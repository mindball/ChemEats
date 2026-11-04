using Domain.Entities;
using Domain.Infrastructure.Identity;
using Domain.Repositories.Obligations;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Shared;
using Shared.DTOs.Obligations;
using WebApi.Infrastructure.Filters;

namespace WebApi.Routes.Obligations;

public static class AdminObligationTypesEndpoints
{
    public static void MapAdminObligationTypeEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(ApiRoutes.AdminObligationTypes.Base)
            .RequireAuthorization("AdminPolicy");

        group.MapGet("/", GetObligationTypesAsync)
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
        group.MapPost("/", CreateObligationTypeAsync)
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
        group.MapPut(ApiRoutes.AdminObligationTypes.ByIdRoute, RenameObligationTypeAsync)
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
        group.MapDelete(ApiRoutes.AdminObligationTypes.ByIdRoute, DeleteObligationTypeAsync)
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
    }

    private static async Task<IResult> GetObligationTypesAsync(
        IObligationTypeRepository obligationTypeRepository,
        IMapper mapper,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<ObligationType> types = await obligationTypeRepository.GetAllAsync(cancellationToken);
            List<ObligationTypeDto> dtos = types.Select(mapper.Map<ObligationTypeDto>).ToList();

            logger.LogInformation("Admin retrieved {TypeCount} expense types", dtos.Count);

            return Results.Ok(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving expense types: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    private static async Task<IResult> CreateObligationTypeAsync(
        SaveObligationTypeDto? requestDto,
        IObligationTypeRepository obligationTypeRepository,
        UserManager<ApplicationUser> userManager,
        HttpContext httpContext,
        IMapper mapper,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            string? validationError = ValidateName(requestDto?.Name);
            if (validationError is not null)
            {
                logger.LogWarning("Create expense type rejected - {ValidationError}", validationError);
                return Results.BadRequest(validationError);
            }

            ApplicationUser? admin = await userManager.GetUserAsync(httpContext.User);
            if (admin is null)
                return Results.Unauthorized();

            if (await obligationTypeRepository.NameExistsAsync(requestDto!.Name, cancellationToken: cancellationToken))
            {
                logger.LogWarning("Create expense type rejected - name {TypeName} already exists", requestDto.Name);
                return Results.Conflict("An expense type with this name already exists.");
            }

            ObligationType obligationType = ObligationType.Create(requestDto.Name);
            await obligationTypeRepository.AddAsync(obligationType, cancellationToken);

            logger.LogInformation(
                "Admin {AdminName} ({AdminId}) created expense type {TypeId} {TypeName}",
                admin.UserName,
                admin.Id,
                obligationType.Id,
                obligationType.Name);

            return Results.Created(ApiRoutes.AdminObligationTypes.ById(obligationType.Id), mapper.Map<ObligationTypeDto>(obligationType));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating expense type: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    private static async Task<IResult> RenameObligationTypeAsync(
        Guid obligationTypeId,
        SaveObligationTypeDto? requestDto,
        IObligationTypeRepository obligationTypeRepository,
        UserManager<ApplicationUser> userManager,
        HttpContext httpContext,
        IMapper mapper,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            string? validationError = ValidateName(requestDto?.Name);
            if (validationError is not null)
            {
                logger.LogWarning("Rename expense type {TypeId} rejected - {ValidationError}", obligationTypeId, validationError);
                return Results.BadRequest(validationError);
            }

            ApplicationUser? admin = await userManager.GetUserAsync(httpContext.User);
            if (admin is null)
                return Results.Unauthorized();

            if (await obligationTypeRepository.NameExistsAsync(requestDto!.Name, obligationTypeId, cancellationToken))
            {
                logger.LogWarning("Rename expense type {TypeId} rejected - name {TypeName} already exists", obligationTypeId, requestDto.Name);
                return Results.Conflict("An expense type with this name already exists.");
            }

            bool renamed = await obligationTypeRepository.RenameAsync(obligationTypeId, requestDto.Name, cancellationToken);
            if (!renamed)
            {
                logger.LogWarning("Rename expense type rejected - type {TypeId} not found", obligationTypeId);
                return Results.NotFound("Expense type not found.");
            }

            ObligationType? obligationType = await obligationTypeRepository.GetByIdAsync(obligationTypeId, cancellationToken);
            if (obligationType is null)
                return Results.NotFound("Expense type not found.");

            logger.LogInformation(
                "Admin {AdminName} ({AdminId}) renamed expense type {TypeId} to {TypeName}",
                admin.UserName,
                admin.Id,
                obligationTypeId,
                obligationType.Name);

            return Results.Ok(mapper.Map<ObligationTypeDto>(obligationType));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error renaming expense type {TypeId}: {ErrorMessage}", obligationTypeId, ex.Message);
            throw;
        }
    }

    private static async Task<IResult> DeleteObligationTypeAsync(
        Guid obligationTypeId,
        IObligationTypeRepository obligationTypeRepository,
        UserManager<ApplicationUser> userManager,
        HttpContext httpContext,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            ApplicationUser? admin = await userManager.GetUserAsync(httpContext.User);
            if (admin is null)
                return Results.Unauthorized();

            // Soft delete: existing obligations keep the type name they were created with.
            bool deleted = await obligationTypeRepository.SoftDeleteAsync(obligationTypeId, cancellationToken);
            if (!deleted)
            {
                logger.LogWarning("Delete expense type rejected - type {TypeId} not found", obligationTypeId);
                return Results.NotFound("Expense type not found.");
            }

            logger.LogInformation(
                "Admin {AdminName} ({AdminId}) deleted expense type {TypeId}",
                admin.UserName,
                admin.Id,
                obligationTypeId);

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting expense type {TypeId}: {ErrorMessage}", obligationTypeId, ex.Message);
            throw;
        }
    }

    private static string? ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Name is required.";

        if (name.Trim().Length > ObligationType.NameMaxLength)
            return $"Name cannot be longer than {ObligationType.NameMaxLength} characters.";

        return null;
    }
}
