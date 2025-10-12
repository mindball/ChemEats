using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Domain.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public EmployeeId? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
}