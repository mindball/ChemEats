using Domain.Entities;
using Domain.Repositories.Suppliers;
using MapsterMapper;
using Shared.DTOs.Suppliers;

namespace WebApi.Routes.Suppliers;

public static class SupplierEndpoints
{
    public static void MapSupplierEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/suppliers");

        // GET all
        group.MapGet("/", async (
            ISupplierRepository repo,
            IMapper mapper,
            CancellationToken cancellationToken) =>
        {
            IEnumerable<Supplier> suppliers = await repo.GetAllAsync(cancellationToken);
            IEnumerable<SupplierDto> dto =  mapper.Map<IEnumerable<SupplierDto>>(suppliers);
            return Results.Ok(dto);
        });

        // GET by id
        group.MapGet("/{id:guid}", async (
            Guid id,
            ISupplierRepository repo,
            IMapper mapper,
            CancellationToken cancellationToken) =>
        {
            Supplier? supplier = await repo.GetByIdAsync(new SupplierId(id), cancellationToken);
            return supplier is not null
                ? Results.Ok(mapper.Map<SupplierDto>(supplier))
                : Results.NotFound();
        });

        // POST
        group.MapPost("/", async (
            CreateSupplierDto dto,
            ISupplierRepository repo,
            IMapper mapper,
            CancellationToken cancellationToken) =>
        {
            Supplier entity = mapper.Map<Supplier>(dto);
            await repo.AddAsync(entity, cancellationToken);
            return Results.Created($"/api/suppliers/{entity.Id.Value}", mapper.Map<SupplierDto>(entity));
        });

        // PUT
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateSupplierDto dto,
            ISupplierRepository repo,
            IMapper mapper,
            CancellationToken cancellationToken) =>
        {
            Supplier? existing = await repo.GetByIdAsync(new SupplierId(id), cancellationToken);
            if (existing is null)
                return Results.NotFound();

            mapper.Map(dto, existing); // Map updates onto existing entity
            await repo.UpdateAsync(existing, cancellationToken);

            return Results.Ok(mapper.Map<SupplierDto>(existing));
        });

        // DELETE
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISupplierRepository repo,
            CancellationToken cancellationToken) =>
        {
            Supplier? supplier = await repo.GetByIdAsync(new SupplierId(id), cancellationToken);
            if (supplier is null)
                return Results.NotFound();

            await repo.DeleteAsync(supplier, cancellationToken);
            return Results.NoContent();
        });
    }
}