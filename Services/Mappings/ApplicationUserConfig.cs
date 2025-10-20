using Domain.Infrastructure.Identity;
using Mapster;
using Shared.DTOs.Employees;

public class ApplicationUserConfig : IRegister
{
    // public void Register(TypeAdapterConfig config)
    // {
    //     config.NewConfig<EmployeeDto, Employee>()
    //         .ConstructUsing(dto => new Employee(new EmployeeId(dto.Id), dto.FullName, dto.Abbreviation))
    //         .IgnoreNullValues(true);
    //
    //     config.NewConfig<Employee, EmployeeDto>()
    //         .Map(dest => dest.Id, src => src.Id.Value)
    //         .Map(dest => dest.FullName, src => src.FullName)
    //         .Map(dest => dest.Abbreviation, src => src.Abbreviation);
    //
    //     
    //     config.NewConfig<EmployeeDto, ApplicationUser>()
    //         .Map(dest => dest.EmployeeId, src => (EmployeeId?)new EmployeeId(src.Id));
    //
    //    
    // }
    // Ако по-късно добавиш IsActive свойство и в Employee(например public bool IsActive { get; private set; }), тогава просто можеш да го мапнеш и обратно:
    //
    // .Map(dest => dest.IsActive, src => src.IsActive)

    public void Register(TypeAdapterConfig config)
    {
        TypeAdapterConfig<UserDto, ApplicationUser>.NewConfig()
            .Map(dest => dest.Abbreviation, src => src.Code)
            .Map(dest => dest.UserName, src => src.Code)
            .Map(dest => dest.FullName, src => src.Name)
            .IgnoreNonMapped(true);

        // ApplicationUser -> UserDto (обратна)
        TypeAdapterConfig<ApplicationUser, UserDto>.NewConfig()
            .Map(dest => dest.Code, src => src.Abbreviation)
            .Map(dest => dest.Code, src => src.UserName)
            .Map(dest => dest.Name, src => src.FullName)
            .IgnoreNonMapped(true);
    }
}