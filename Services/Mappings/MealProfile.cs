using AutoMapper;
using Domain.Entities;
using Shared.DTOs.Meals;

namespace Services.Mappings;

public class MealProfile : Profile
{
    public MealProfile()
    {
        // Domain → DTO
        // CreateMap<Meal, MealDto>()
        //     .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.Value))
        //     .ForMember(dest => dest.Price, opt => opt.MapFrom(src => (decimal)src.Price));
        //
        // // DTO → Domain
        // CreateMap<MealDto, Meal>()
        //     .ConstructUsing(dto =>
        //         new Meal(
        //             dto.Id.HasValue ? new MealId(dto.Id.Value) : MealId.New(),
        //             dto.Name,
        //             (Price)dto.Price 
        //         ));
    }
}