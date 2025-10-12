using Domain.Entities;
using Domain.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("AspNetUsers"); // reuse the Identity table

        builder.HasKey(u => u.Id);

        builder.Property(u => u.UserName)
            .HasMaxLength(50);

        builder.Property(u => u.Email)
            .HasMaxLength(100);

        builder.Property(u => u.EmployeeId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new EmployeeId(value.Value) : (EmployeeId?)null
            )
            .IsRequired(false);


        builder
            .HasOne(u => u.Employee)
            .WithOne(e => e.User)
            .HasForeignKey<ApplicationUser>(u => u.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => u.EmployeeId).IsUnique();
    }
}