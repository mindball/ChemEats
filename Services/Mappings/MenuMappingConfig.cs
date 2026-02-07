    using Domain.Entities;
    using Mapster;
    using Shared.DTOs.Meals;
    using Shared.DTOs.Menus;

    namespace Services.Mappings;

    public class MenuMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Menu, MenuDto>()
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.SupplierId, src => src.SupplierId)
                .Map(dest => dest.SupplierName, src => src.Supplier != null ? src.Supplier.Name : string.Empty)
                .Map(dest => dest.Date, src => src.Date)
                .Map(dest => dest.ActiveUntil, src => src.ActiveUntil)
                .Map(dest => dest.IsActive, src => src.IsActive())
                .Map(dest => dest.IsDeleted, src => src.IsDeleted)
                .Map(dest => dest.Meals, src => src.Meals);

        config.NewConfig<Menu, MenuDto>()
            .Map(dest => dest.SupplierName, src => src.Supplier != null ? src.Supplier.Name : string.Empty)
            .Map(dest => dest.Date, src => src.Date)
            .Map(dest => dest.Meals, src => src.Meals.Adapt<List<MealDto>>())
            .Map(dest => dest.Id, src => src.Id)
            // .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.IsDeleted, src => src.IsDeleted);

        config.NewConfig<MenuDto, Menu>()
            .ConstructUsing(src =>
                new Menu(
                    src.Id,
                    src.SupplierId,
                    src.Date,
                    src.ActiveUntil,
                    src.Meals.Adapt<List<Meal>>()
                )
            );

        config.NewConfig<UpdateMenuDto, Menu>()
            .AfterMapping((src, dest) =>
            {
                dest.GetType().GetProperty(nameof(Menu.SupplierId))?.SetValue(dest, src.SupplierId);
                dest.GetType().GetProperty(nameof(Menu.Date))?.SetValue(dest, src.Date);
            });
    }
    }