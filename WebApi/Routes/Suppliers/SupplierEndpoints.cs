using Domain.Entities;
using Domain.Repositories.Suppliers;
using MapsterMapper;
using Shared.DTOs.Suppliers;
using WebApi.Infrastructure.Filters;

namespace WebApi.Routes.Suppliers;

public static class SupplierEndpoints
{
    public static void MapSupplierEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/suppliers")
            .RequireAuthorization();

        // GET all
        group.MapGet("/", async (
            ISupplierRepository repo,
            IMapper mapper,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
        {
            try
            {
                logger.LogInformation("Retrieving all suppliers");

                IEnumerable<Supplier> suppliers = await repo.GetAllAsync(cancellationToken);
                IEnumerable<SupplierDto> dto = mapper.Map<IEnumerable<SupplierDto>>(suppliers);

                logger.LogInformation(
                    "Suppliers loaded successfully");

                return Results.Ok(dto);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Error retrieving all suppliers: {ErrorMessage}",
                    ex.Message);
                throw;
            }
        }).RequireAuthorization().AddEndpointFilter<AuthorizedRequestLoggingFilter>();

        // GET by id
        group.MapGet("/{id:guid}", async (
            Guid id,
            ISupplierRepository repo,
            IMapper mapper,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
        {
            try
            {
                logger.LogInformation("Retrieving supplier by ID: {SupplierId}", id);

                Supplier? supplier = await repo.GetByIdAsync(id, cancellationToken);
                
                if (supplier is not null)
                {
                    logger.LogInformation(
                        "Supplier {SupplierId} ({SupplierName}) found successfully",
                        supplier.Id,
                        supplier.Name);
                    return Results.Ok(mapper.Map<SupplierDto>(supplier));
                }

                logger.LogWarning("Supplier {SupplierId} not found", id);
                return Results.NotFound();
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Error retrieving supplier {SupplierId}: {ErrorMessage}",
                    id,
                    ex.Message);
                throw;
            }
        }).RequireAuthorization().AddEndpointFilter<AuthorizedRequestLoggingFilter>();

        // POST
        group.MapPost("/", async (
            CreateSupplierDto dto,
            ISupplierRepository repo,
            IMapper mapper,
            ILogger<Program> logger,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            try
            {
                logger.LogInformation(
                    "User {User} creating new supplier: {SupplierName}",
                    context.User.Identity?.Name,
                    dto.Name);

                Supplier entity = mapper.Map<Supplier>(dto);

                if (!string.IsNullOrEmpty(dto.SupervisorId))
                    entity.AssignSupervisor(dto.SupervisorId);

                await repo.AddAsync(entity, cancellationToken);

                logger.LogInformation(
                    "Supplier {SupplierId} ({SupplierName}) created successfully by {User}",
                    entity.Id,
                    entity.Name,
                    context.User.Identity?.Name);

                return Results.Created($"/api/suppliers/{entity.Id}", mapper.Map<SupplierDto>(entity));
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Error creating supplier {SupplierName}: {ErrorMessage}",
                    dto.Name,
                    ex.Message);
                throw;
            }
        }).RequireAuthorization().AddEndpointFilter<AuthorizedRequestLoggingFilter>();

        // PUT
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateSupplierDto dto,
            ISupplierRepository repo,
            IMapper mapper,
            ILogger<Program> logger,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            try
            {
                logger.LogInformation(
                    "User {User} updating supplier {SupplierId}",
                    context.User.Identity?.Name,
                    id);

                Supplier? existing = await repo.GetByIdAsync(id, cancellationToken);
                if (existing is null)
                {
                    logger.LogWarning(
                        "Supplier {SupplierId} not found for update by {User}",
                        id,
                        context.User.Identity?.Name);
                    return Results.NotFound();
                }

                string oldName = existing.Name;
                mapper.Map(dto, existing);

                if (!string.IsNullOrEmpty(dto.SupervisorId))
                    existing.AssignSupervisor(dto.SupervisorId);
                else
                    existing.RemoveSupervisor();

                await repo.UpdateAsync(existing, cancellationToken);

                logger.LogInformation(
                    "Supplier {SupplierId} updated successfully by {User} (Name: {OldName} -> {NewName})",
                    id,
                    context.User.Identity?.Name,
                    oldName,
                    existing.Name);

                return Results.Ok(mapper.Map<SupplierDto>(existing));
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Error updating supplier {SupplierId}: {ErrorMessage}",
                    id,
                    ex.Message);
                throw;
            }
        }).RequireAuthorization().AddEndpointFilter<AuthorizedRequestLoggingFilter>();

        // DELETE
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISupplierRepository repo,
            ILogger<Program> logger,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            try
            {
                logger.LogWarning(
                    "User {User} attempting to delete supplier {SupplierId}",
                    context.User.Identity?.Name,
                    id);

                Supplier? supplier = await repo.GetByIdAsync(id, cancellationToken);
                if (supplier is null)
                {
                    logger.LogWarning(
                        "Supplier {SupplierId} not found for deletion by {User}",
                        id,
                        context.User.Identity?.Name);
                    return Results.NotFound();
                }

                string supplierName = supplier.Name;
                await repo.DeleteAsync(supplier, cancellationToken);

                logger.LogInformation(
                    "Supplier {SupplierId} ({SupplierName}) deleted successfully by {User}",
                    id,
                    supplierName,
                    context.User.Identity?.Name);

                return Results.NoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Error deleting supplier {SupplierId}: {ErrorMessage}",
                    id,
                    ex.Message);
                throw;
            }
        }).RequireAuthorization().AddEndpointFilter<AuthorizedRequestLoggingFilter>();
    }
}