using Domain.Entities;
using Domain.Infrastructure.Identity;
using Domain.Models.Obligations;
using Domain.Repositories.Obligations;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Shared;
using Shared.DTOs.Obligations;
using WebApi.Infrastructure.Filters;

namespace WebApi.Routes.Obligations;

public static class AdminObligationsEndpoints
{
    public static void MapAdminObligationEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup(ApiRoutes.AdminObligations.Base)
            .RequireAuthorization("AdminPolicy");

        group.MapGet(ApiRoutes.AdminObligations.ByUserRoute, GetUserObligationsAsync)
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
        group.MapPost("/", CreateObligationAsync)
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
        group.MapPut(ApiRoutes.AdminObligations.ByIdRoute, UpdateObligationAsync)
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
        group.MapDelete(ApiRoutes.AdminObligations.ByIdRoute, DeleteObligationAsync)
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
        group.MapPost(ApiRoutes.AdminObligations.Pay, PayObligationsAsync)
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
        group.MapPost(ApiRoutes.AdminObligations.PayAll, PayAllUnpaidObligationsAsync)
            .AddEndpointFilter<AuthorizedRequestLoggingFilter>();
    }

    private static async Task<IResult> GetUserObligationsAsync(
        string userId,
        DateTime? startDate,
        DateTime? endDate,
        IOtherObligationRepository obligationRepository,
        IMapper mapper,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                logger.LogWarning("Get obligations rejected - user ID is required");
                return Results.BadRequest("User ID is required.");
            }

            if (startDate.HasValue && endDate.HasValue && endDate.Value.Date < startDate.Value.Date)
            {
                logger.LogWarning(
                    "Get obligations rejected for user {UserId} - end date {EndDate} is before start date {StartDate}",
                    userId,
                    endDate.Value.ToString("yyyy-MM-dd"),
                    startDate.Value.ToString("yyyy-MM-dd"));
                return Results.BadRequest("End date cannot be before start date.");
            }

            IReadOnlyList<OtherObligationItem> items = await obligationRepository.GetForUserAsync(
                userId,
                startDate,
                endDate,
                cancellationToken: cancellationToken);

            List<OtherObligationDto> dtos = items.Select(mapper.Map<OtherObligationDto>).ToList();

            logger.LogInformation(
                "Admin retrieved {ObligationCount} other obligations for user {UserId} - Period: {StartDate} to {EndDate}",
                dtos.Count,
                userId,
                startDate?.ToString("yyyy-MM-dd") ?? "None",
                endDate?.ToString("yyyy-MM-dd") ?? "None");

            return Results.Ok(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error retrieving other obligations for user {UserId}: {ErrorMessage}",
                userId,
                ex.Message);
            throw;
        }
    }

    private static async Task<IResult> CreateObligationAsync(
        CreateOtherObligationDto? requestDto,
        IOtherObligationRepository obligationRepository,
        IObligationTypeRepository obligationTypeRepository,
        UserManager<ApplicationUser> userManager,
        HttpContext httpContext,
        IMapper mapper,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            if (requestDto is null)
            {
                logger.LogWarning("Create obligation rejected - request body is missing");
                return Results.BadRequest("Request body is required.");
            }

            if (string.IsNullOrWhiteSpace(requestDto.UserId))
            {
                logger.LogWarning("Create obligation rejected - user ID is required");
                return Results.BadRequest("User ID is required.");
            }

            string? validationError = ValidateFields(requestDto.ObligationTypeId, requestDto.Amount, requestDto.Date);
            if (validationError is not null)
            {
                logger.LogWarning("Create obligation rejected for user {UserId} - {ValidationError}",
                    requestDto.UserId,
                    validationError);
                return Results.BadRequest(validationError);
            }

            ApplicationUser? admin = await userManager.GetUserAsync(httpContext.User);
            if (admin is null)
                return Results.Unauthorized();

            ApplicationUser? employee = await userManager.FindByIdAsync(requestDto.UserId);
            if (employee is null)
            {
                logger.LogWarning("Create obligation rejected - employee {UserId} not found", requestDto.UserId);
                return Results.NotFound("Employee not found.");
            }

            ObligationType? obligationType = await obligationTypeRepository.GetByIdAsync(requestDto.ObligationTypeId, cancellationToken);
            if (obligationType is null)
            {
                logger.LogWarning("Create obligation rejected - expense type {TypeId} not found", requestDto.ObligationTypeId);
                return Results.BadRequest("Unknown expense type.");
            }

            OtherObligation obligation = OtherObligation.Create(
                employee.Id,
                obligationType,
                requestDto.Amount,
                requestDto.Date,
                DateTime.Now,
                admin.Id);

            await obligationRepository.AddAsync(obligation, cancellationToken);

            OtherObligationItem? created = await obligationRepository.GetItemByIdAsync(obligation.Id, cancellationToken);
            if (created is null)
                return Results.Problem("The obligation was saved but could not be loaded.");

            logger.LogInformation(
                "Admin {AdminName} ({AdminId}) created other obligation {ObligationId} for user {UserId}: {Amount}",
                admin.UserName,
                admin.Id,
                obligation.Id,
                employee.Id,
                obligation.Amount);

            return Results.Created(ApiRoutes.AdminObligations.ById(obligation.Id), mapper.Map<OtherObligationDto>(created));
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error creating other obligation for user {UserId}: {ErrorMessage}",
                requestDto?.UserId,
                ex.Message);
            throw;
        }
    }

    private static async Task<IResult> UpdateObligationAsync(
        Guid obligationId,
        UpdateOtherObligationDto? requestDto,
        IOtherObligationRepository obligationRepository,
        IObligationTypeRepository obligationTypeRepository,
        UserManager<ApplicationUser> userManager,
        HttpContext httpContext,
        IMapper mapper,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            if (requestDto is null)
            {
                logger.LogWarning("Update obligation {ObligationId} rejected - request body is missing", obligationId);
                return Results.BadRequest("Request body is required.");
            }

            string? validationError = ValidateFields(requestDto.ObligationTypeId, requestDto.Amount, requestDto.Date);
            if (validationError is not null)
            {
                logger.LogWarning("Update obligation {ObligationId} rejected - {ValidationError}",
                    obligationId,
                    validationError);
                return Results.BadRequest(validationError);
            }

            ApplicationUser? admin = await userManager.GetUserAsync(httpContext.User);
            if (admin is null)
                return Results.Unauthorized();

            ObligationType? obligationType = await obligationTypeRepository.GetByIdAsync(requestDto.ObligationTypeId, cancellationToken);
            if (obligationType is null)
            {
                logger.LogWarning("Update obligation {ObligationId} rejected - expense type {TypeId} not found",
                    obligationId,
                    requestDto.ObligationTypeId);
                return Results.BadRequest("Unknown expense type.");
            }

            bool updated = await obligationRepository.UpdateAsync(
                obligationId,
                obligationType,
                requestDto.Amount,
                requestDto.Date,
                cancellationToken);

            if (!updated)
            {
                logger.LogWarning("Update obligation rejected - obligation {ObligationId} not found", obligationId);
                return Results.NotFound("Obligation not found.");
            }

            OtherObligationItem? item = await obligationRepository.GetItemByIdAsync(obligationId, cancellationToken);
            if (item is null)
                return Results.NotFound("Obligation not found.");

            logger.LogInformation(
                "Admin {AdminName} ({AdminId}) updated other obligation {ObligationId}: {Amount}",
                admin.UserName,
                admin.Id,
                obligationId,
                item.Amount);

            return Results.Ok(mapper.Map<OtherObligationDto>(item));
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error updating other obligation {ObligationId}: {ErrorMessage}",
                obligationId,
                ex.Message);
            throw;
        }
    }

    private static async Task<IResult> DeleteObligationAsync(
        Guid obligationId,
        IOtherObligationRepository obligationRepository,
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

            bool deleted = await obligationRepository.SoftDeleteAsync(obligationId, cancellationToken);
            if (!deleted)
            {
                logger.LogWarning("Delete obligation rejected - obligation {ObligationId} not found", obligationId);
                return Results.NotFound("Obligation not found.");
            }

            logger.LogInformation(
                "Admin {AdminName} ({AdminId}) deleted other obligation {ObligationId}",
                admin.UserName,
                admin.Id,
                obligationId);

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error deleting other obligation {ObligationId}: {ErrorMessage}",
                obligationId,
                ex.Message);
            throw;
        }
    }

    private static async Task<IResult> PayObligationsAsync(
        ObligationPayRequestDto? requestDto,
        IOtherObligationRepository obligationRepository,
        UserManager<ApplicationUser> userManager,
        HttpContext httpContext,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            if (requestDto is null || string.IsNullOrWhiteSpace(requestDto.UserId))
            {
                logger.LogWarning("Obligation pay rejected - user ID is required");
                return Results.BadRequest("User ID is required.");
            }

            if (requestDto.ObligationIds is null || requestDto.ObligationIds.Count == 0)
            {
                logger.LogWarning("Obligation pay rejected - empty obligation IDs list");
                return Results.BadRequest("At least one obligation ID is required.");
            }

            ApplicationUser? admin = await userManager.GetUserAsync(httpContext.User);
            if (admin is null)
                return Results.Unauthorized();

            (int paidCount, decimal totalPaid) = await obligationRepository.MarkAsPaidAsync(
                requestDto.UserId,
                requestDto.ObligationIds,
                DateTime.Now,
                admin.Id,
                cancellationToken);

            logger.LogInformation(
                "Admin {AdminName} ({AdminId}) paid {PaidCount} of {RequestedCount} other obligations for user {UserId}, total {TotalPaid}",
                admin.UserName,
                admin.Id,
                paidCount,
                requestDto.ObligationIds.Count,
                requestDto.UserId,
                totalPaid);

            return Results.Ok(new ObligationPayResponseDto(paidCount, totalPaid));
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error paying other obligations for user {UserId}: {ErrorMessage}",
                requestDto?.UserId,
                ex.Message);
            throw;
        }
    }

    private static async Task<IResult> PayAllUnpaidObligationsAsync(
        ObligationPayAllRequestDto? requestDto,
        IOtherObligationRepository obligationRepository,
        UserManager<ApplicationUser> userManager,
        HttpContext httpContext,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            if (requestDto is null || string.IsNullOrWhiteSpace(requestDto.UserId))
            {
                logger.LogWarning("Pay all obligations rejected - user ID is required");
                return Results.BadRequest("User ID is required.");
            }

            ApplicationUser? admin = await userManager.GetUserAsync(httpContext.User);
            if (admin is null)
                return Results.Unauthorized();

            (int paidCount, decimal totalPaid) = await obligationRepository.MarkAllUnpaidAsPaidAsync(
                requestDto.UserId,
                requestDto.UpToDate,
                DateTime.Now,
                admin.Id,
                cancellationToken);

            logger.LogInformation(
                "Admin {AdminName} ({AdminId}) paid all unpaid other obligations up to {UpToDate} for user {UserId}: {PaidCount} obligations, total {TotalPaid}",
                admin.UserName,
                admin.Id,
                requestDto.UpToDate?.ToString("yyyy-MM-dd") ?? "any date",
                requestDto.UserId,
                paidCount,
                totalPaid);

            return Results.Ok(new ObligationPayResponseDto(paidCount, totalPaid));
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error paying all other obligations for user {UserId}: {ErrorMessage}",
                requestDto?.UserId,
                ex.Message);
            throw;
        }
    }

    private static string? ValidateFields(Guid obligationTypeId, decimal amount, DateTime date)
    {
        if (obligationTypeId == Guid.Empty)
            return "Expense type is required.";

        if (amount <= 0m)
            return "Amount must be greater than zero.";

        if (decimal.Round(amount, 2) != amount)
            return "Amount cannot have more than two decimal places.";

        if (date == default)
            return "Date is required.";

        return null;
    }
}
