using Domain.Repositories.Employees;
using Domain.Repositories.MealOrders;
using Domain.Repositories.Meals;
using Domain.Repositories.Menus;
using Domain.Repositories.Suppliers;
using Services.Repositories.Employees;
using Services.Repositories.MealOrders;
using Services.Repositories.Meals;
using Services.Repositories.Menus;
using Services.Repositories.Suppliers;


namespace WebApi.Extensions.DI;

public static class RepositoriesExtension
{
    internal static IServiceCollection RegisterRepositories(this IServiceCollection services)
    {
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IMealOrderRepository, MealOrderRepository>();
        services.AddScoped<IMealRepository, MealRepository>();
        services.AddScoped<IMenuRepository, MenuRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        return services;
    }
}