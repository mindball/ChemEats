using Domain.Entities;
using Mapster;
using Shared.DTOs.Meals;
using Shared.DTOs.Menus;

namespace Services.Mappings;

public class MenuMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        

        config.NewConfig<CreateMenuDto, Menu>()
            .ConstructUsing(src =>
                Menu.Create(
                    src.SupplierId,
                    src.Date,
                    src.Meals.Adapt<List<Meal>>()
                )
            );

        config.NewConfig<Menu, MenuDto>()
            .Map(dest => dest.SupplierName, src => src.Supplier != null ? src.Supplier.Name : string.Empty)
            .Map(dest => dest.Date, src => src.Date)
            .Map(dest => dest.Meals, src => src.Meals.Adapt<List<MealDto>>());

        config.NewConfig<MenuDto, Menu>()
            .ConstructUsing(src =>
                new Menu(
                    src.Id.HasValue ? src.Id.Value : Guid.NewGuid(),
                    src.SupplierId,
                    src.Date,
                    src.Meals.Adapt<List<Meal>>()
                )
            );

        config.NewConfig<UpdateMenuDto, Menu>()
            .AfterMapping((src, dest) =>
            {
                dest.GetType().GetProperty(nameof(Menu.SupplierId))?.SetValue(dest,src.SupplierId);
                dest.GetType().GetProperty(nameof(Menu.Date))?.SetValue(dest, src.Date);
            });
    }
}