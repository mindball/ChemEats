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
            .HasColumnType("VARCHAR(36)")
            .ValueGeneratedNever();

        builder.Property(m => m.SupplierId)
            .IsRequired();

        builder.Property<Guid>(m => m.SupplierId)
            .HasColumnType("VARCHAR(36)")
            .IsRequired();

        builder.Property(m => m.Date)
            .IsRequired();

        // builder.Property(m => m.IsActive)
        //     .HasDefaultValue(true)
        //     .IsRequired();

        builder.Property(m => m.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasMany(m => m.Meals)
            .WithOne(m => m.Menu)
            .HasForeignKey(m => m.MenuId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

