using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations;

public class MealOrderConfiguration : IEntityTypeConfiguration<MealOrder>
{
    public void Configure(EntityTypeBuilder<MealOrder> builder)
    {
        builder.HasKey(mo => mo.Id);

        builder.Property(mo => mo.Id)
            .ValueGeneratedNever();

        builder.Property(mo => mo.MealId)
            .IsRequired();

        builder.Property(mo => mo.Date)
            .IsRequired();

        builder.HasOne(mo => mo.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(mo => mo.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mo => mo.Meal)
            .WithMany()
            .HasForeignKey(mo => mo.MealId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}