using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations;

public class MealConfiguration : IEntityTypeConfiguration<Meal>
{
    public void Configure(EntityTypeBuilder<Meal> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnType("VARCHAR(36)")
            .ValueGeneratedNever();

        builder.Property(m => m.MenuId)
            .HasColumnType("VARCHAR(36)")
            .IsRequired();

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(100);


        builder.HasOne(m => m.Menu)
            .WithMany(m => m.Meals)
            .HasForeignKey(m => m.MenuId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(m => m.Price, price =>
        {
            price.Property(p => p.Amount)
                .HasColumnName("Price_Amount")
                .HasPrecision(10, 2)
                .IsRequired();
        });
    }
}