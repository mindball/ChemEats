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
            .ValueGeneratedNever();

        builder.Property(m => m.SupplierId)
            .IsRequired();

        builder.Property(m => m.Date)
            .IsRequired();

        builder.HasMany(m => m.Meals)
            .WithOne()
            .HasForeignKey("MenuId");
    }
}

