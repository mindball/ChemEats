using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations;

public class MenuConfiguration : IEntityTypeConfiguration<Menu>
{
    public void Configure(EntityTypeBuilder<Menu> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasConversion(id => id.Value, value => new MenuId(value))
            .ValueGeneratedNever();

        builder.Property(m => m.SupplierId)
            .HasConversion(id => id.Value, value => new SupplierId(value))
            .IsRequired();

        builder.Property(m => m.Date)
            .IsRequired();

        builder.HasMany(m => m.Meals)
            .WithOne()
            .HasForeignKey("MenuId");
    }
}

