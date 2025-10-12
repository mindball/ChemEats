using Domain;
using Domain.Entities;
using Domain.Repositories.Employees;

namespace Services.Repositories.Employees;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _appDbContext;
    public EmployeeRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }
    
    public Task<Employee?> GetByIdAsync(EmployeeId Id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task AddAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}