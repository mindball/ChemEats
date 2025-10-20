using Domain.Infrastructure.Identity;
using Domain.Repositories.Employees;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Shared.DTOs.Employees;

namespace WebApi.Infrastructure.Employees;

public static class IdentitySeeder
{
    // DELETE FROM "AspNetUserRoles";
    // DELETE FROM "AspNetRoles";
    // DELETE FROM "AspNetUsers"
    // DELETE FROM "Employees"
    public static async Task SeedAsync(
        IUserRepository userRepository,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IEmployeeExternalService employeeService)
    {
        //Make its public const
        string[] roles = { "Admin", "Employee", "Manager" };
        string[] adminEmployeeCodes = { "MM", "DM" };

        foreach (string role in roles)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        List<UserDto> users = await employeeService.GetAllEmployeesAsync();

        foreach (UserDto dto in users)
        {
            ApplicationUser? existingEmployee = await userManager.FindByNameAsync(dto.Code);
            
            if (existingEmployee == null)
            {
                //     ApplicationUser user = new()
                //     {
                //         UserName = employee.Abbreviation,
                //         Email = $"{employee.Abbreviation}@cpachem.com",
                //         EmailConfirmed = true
                //     };
                ApplicationUser newUser = dto.Adapt<ApplicationUser>();
                var result = await userManager.CreateAsync(newUser, dto.Code);
                string roleToAssign = adminEmployeeCodes.Contains(newUser.Abbreviation) ? "Admin" : "Employee";
                await userManager.AddToRoleAsync(newUser, roleToAssign);
            }
        }
    }
}