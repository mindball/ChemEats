using Domain.Entities;
using Mapster;
using Shared.DTOs.Meals;
using Shared.DTOs.Menus;

namespace Services.Mappings;

public class MenuMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<MenuId, Guid>()
            .MapWith(src => src.Value);
        config.NewConfig<Guid, MenuId>()
            .MapWith(src => new MenuId(src));

        config.NewConfig<SupplierId, Guid>()
            .MapWith(src => src.Value);
        config.NewConfig<Guid, SupplierId>()
            .MapWith(src => new SupplierId(src));

        config.NewConfig<CreateMenuDto, Menu>()
            .ConstructUsing(src =>
                Menu.Create(
                    new SupplierId(src.SupplierId),
                    src.Date,
                    src.Meals.Adapt<List<Meal>>()
                )
            );

        config.NewConfig<Menu, MenuDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.SupplierId, src => src.SupplierId.Value)
            .Map(dest => dest.SupplierName, src => src.Supplier != null ? src.Supplier.Name : string.Empty)
            .Map(dest => dest.Date, src => src.Date)
            .Map(dest => dest.Meals, src => src.Meals.Adapt<List<MealDto>>());

        config.NewConfig<MenuDto, Menu>()
            .ConstructUsing(src =>
                new Menu(
                    src.Id.HasValue ? new MenuId(src.Id.Value) : MenuId.New(),
                    new SupplierId(src.SupplierId),
                    src.Date,
                    src.Meals.Adapt<List<Meal>>()
                )
            );

        config.NewConfig<UpdateMenuDto, Menu>()
            .AfterMapping((src, dest) =>
            {
                dest.GetType().GetProperty(nameof(Menu.SupplierId))?.SetValue(dest, new SupplierId(src.SupplierId));
                dest.GetType().GetProperty(nameof(Menu.Date))?.SetValue(dest, src.Date);
            });
    }
}