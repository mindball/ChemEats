using Domain.Entities;
using Domain.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Domain;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Meal> Meals => Set<Meal>();
    public DbSet<MealOrder> MealOrders => Set<MealOrder>();
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}