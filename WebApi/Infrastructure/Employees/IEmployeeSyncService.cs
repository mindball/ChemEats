namespace WebApi.Infrastructure.Employees;

public interface IEmployeeSyncService
{
    Task SyncEmployeesAsync();
}