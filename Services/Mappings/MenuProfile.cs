using AutoMapper;
using Domain.Entities;
using Shared.DTOs.Meals;
using Shared.DTOs.Menus;

public class MenuProfile : Profile
{
    public MenuProfile()
    {
        // Strongly typed Ids
    //     CreateMap<MenuId, Guid>().ConvertUsing(src => src.Value);
    //     CreateMap<Guid, MenuId>().ConvertUsing(src => new MenuId(src));
    //
    //     CreateMap<MealId, Guid>().ConvertUsing(src => src.Value);
    //     CreateMap<Guid, MealId>().ConvertUsing(src => new MealId(src));
    //
    //     // Domain -> DTO
    //     CreateMap<Menu, MenuDto>()
    //         .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
    //         .ForMember(dest => dest.SupplierId, opt => opt.MapFrom(src => src.SupplierId.Value))
    //         .ForMember(dest => dest.Meals, opt => opt.MapFrom(src => src.Meals));
    //
    //     CreateMap<Meal, MealDto>()
    //         .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
    //         .ForMember(dest => dest.Price, opt => opt.MapFrom(src => (decimal)src.Price));
    //
    //     // DTO -> Domain
    //     CreateMap<MenuDto, Menu>()
    //         .ConstructUsing(dto =>
    //             Menu.Create(
    //                 new SupplierId(dto.SupplierId),
    //                 dto.Date,
    //                 dto.Meals.Select(m => new Meal(
    //                     m.Id.HasValue ? new MealId(m.Id.Value) : MealId.New(),
    //                     m.Name,
    //                     (Price)m.Price
    //                 )).ToList()
    //             )
    //         );
    //
    //     CreateMap<MealDto, Meal>()
    //         .ConstructUsing(dto =>
    //             new Meal(
    //                 dto.Id.HasValue ? new MealId(dto.Id.Value) : MealId.New(),
    //                 dto.Name,
    //                 (Price)dto.Price
    //             )
    //         );
    }
}