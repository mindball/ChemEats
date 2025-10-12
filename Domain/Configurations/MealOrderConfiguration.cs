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
            .HasConversion(id => id.Value, value => new MealOrderId(value))
            .ValueGeneratedNever();

        builder.Property(mo => mo.EmployeeId)
            .HasConversion(id => id.Value, value => new EmployeeId(value))
            .IsRequired();

        builder.Property(mo => mo.MealId)
            .HasConversion(id => id.Value, value => new MealId(value))
            .IsRequired();

        builder.Property(mo => mo.Date)
            .HasConversion(
                d => d.ToDateTime(TimeOnly.MinValue),
                dt => DateOnly.FromDateTime(dt))
            .IsRequired();

        builder.HasIndex(mo => new { mo.EmployeeId, mo.MealId, mo.Date }).IsUnique();
    }
}