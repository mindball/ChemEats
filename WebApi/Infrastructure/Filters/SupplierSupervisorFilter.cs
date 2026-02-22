using Domain.Entities;
using Domain.Infrastructure.Identity;
using Domain.Repositories.Menus;
using Domain.Repositories.Suppliers;
using Microsoft.AspNetCore.Identity;
using Shared.DTOs.Menus;

namespace WebApi.Infrastructure.Filters;

public class SupplierSupervisorFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        HttpContext httpContext = context.HttpContext;

        if (httpContext.User.IsInRole("Admin"))
            return await next(context);

        UserManager<ApplicationUser> userManager =
            httpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();

        ApplicationUser? user = await userManager.GetUserAsync(httpContext.User);
        if (user is null)
            return Results.Unauthorized();

        Guid? supplierId = ResolveSupplierId(context);

        if (supplierId is null)
        {
            supplierId = await ResolveSupplierIdFromMenuAsync(context, httpContext);
        }

        if (supplierId is null)
            return Results.Forbid();

        ISupplierRepository supplierRepository =
            httpContext.RequestServices.GetRequiredService<ISupplierRepository>();

        Supplier? supplier = await supplierRepository.GetByIdAsync(supplierId.Value);
        if (supplier is null)
            return Results.NotFound();

        if (!supplier.IsSupervisor(user.Id))
            return Results.Forbid();

        return await next(context);
    }

    private static Guid? ResolveSupplierId(EndpointFilterInvocationContext context)
    {
        if (context.HttpContext.Request.RouteValues.TryGetValue("supplierId", out object? value)
            && Guid.TryParse(value?.ToString(), out Guid supplierId))
        {
            return supplierId;
        }

        foreach (object? argument in context.Arguments)
        {
            if (argument is CreateMenuDto dto)
                return dto.SupplierId;
        }

        return null;
    }

    private static async Task<Guid?> ResolveSupplierIdFromMenuAsync(
        EndpointFilterInvocationContext context,
        HttpContext httpContext)
    {
        if (context.HttpContext.Request.RouteValues.TryGetValue("menuId", out object? value)
            && Guid.TryParse(value?.ToString(), out Guid menuId))
        {
            IMenuRepository menuRepository =
                httpContext.RequestServices.GetRequiredService<IMenuRepository>();

            Menu? menu = await menuRepository.GetByIdAsync(menuId);
            return menu?.SupplierId;
        }

        return null;
    }
}
