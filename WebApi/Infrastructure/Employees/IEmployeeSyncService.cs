namespace WebApi.Infrastructure.Employees;

[Obsolete("External HR employee integration is disabled: the isolated host has no access to the HR service. Employees are created manually by an admin. Kept for a possible future integration.")]
public interface IEmployeeSyncService
{
    Task SyncEmployeesAsync();
}