using Domain.Entities;
using Domain.Infrastructure.Identity;
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

        builder.Property(s => s.SupervisorId)
            .HasMaxLength(450);

        builder.HasOne(s => s.Supervisor)
            .WithMany(u => u.SupervisedSuppliers)
            .HasForeignKey(s => s.SupervisorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(s => s.SupervisorId);

        builder.Metadata.FindNavigation(nameof(Supplier.Menus))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}