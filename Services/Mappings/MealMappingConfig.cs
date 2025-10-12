using Domain.Entities;
using Mapster;
using Shared.DTOs.Meals;

namespace Services.Mappings;

public class MealMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<MealId, Guid>()
            .MapWith(src => src.Value);
        config.NewConfig<Guid, MealId>()
            .MapWith(src => new MealId(src));

        config.NewConfig<Price, decimal>()
            .MapWith(src => src.Amount);
        config.NewConfig<decimal, Price>()
            .MapWith(src => new Price(src));

        config.NewConfig<CreateMealDto, Meal>()
            .ConstructUsing(src =>
                Meal.Create(src.Name, new Price(src.Price))
            );

        config.NewConfig<Meal, MealDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Price, src => src.Price.Amount);

        config.NewConfig<MealDto, Meal>()
            .ConstructUsing(src =>
                new Meal(
                    new MealId(src.Id),
                    src.Name,
                    new Price(src.Price)
                )
            );

        config.NewConfig<UpdateMealDto, Meal>()
            .AfterMapping((src, dest) =>
            {
                dest.GetType().GetProperty(nameof(Meal.Name))?.SetValue(dest, src.Name);
                dest.ChangePrice(new Price(src.Price));
            });
    }
}