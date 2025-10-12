using Domain.Entities;

namespace Domain.Repositories.Employees;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(EmployeeId Id, CancellationToken cancellationToken = default);
    Task AddAsync(Employee employee, CancellationToken cancellationToken = default);
}