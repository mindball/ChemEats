namespace WebApi.Infrastructure.Employees;

public interface IIdentitySeedService
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
