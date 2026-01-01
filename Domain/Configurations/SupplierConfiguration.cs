using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.Property(m => m.Id)
            .HasColumnType("VARCHAR(36)")
            .ValueGeneratedNever();

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Metadata.FindNavigation(nameof(Supplier.Menus))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}