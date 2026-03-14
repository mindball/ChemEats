using Domain;
using Microsoft.EntityFrameworkCore;

namespace ChemEats.Tests.TestInfrastructure;

internal static class TestDbContextFactory
{
    public static AppDbContext Create()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source=tests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared")
            .Options;

        AppDbContext context = new(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }
}
