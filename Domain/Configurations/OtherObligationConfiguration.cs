using Domain.Entities;
using Domain.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations;

public class OtherObligationConfiguration : IEntityTypeConfiguration<OtherObligation>
{
    public void Configure(EntityTypeBuilder<OtherObligation> builder)
    {
        builder.ToTable("OtherObligations");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasColumnType("VARCHAR(36)")
            .ValueGeneratedNever();

        builder.Property(o => o.UserId)
            .IsRequired();

        builder.Property<Guid?>(o => o.ObligationTypeId)
            .HasColumnType("VARCHAR(36)")
            .IsRequired(false);

        builder.Property(o => o.Description)
            .HasMaxLength(OtherObligation.DescriptionMaxLength)
            .IsRequired();

        builder.Property(o => o.Amount)
            .HasColumnType("DECIMAL(18,2)")
            .IsRequired();

        builder.Property(o => o.Date)
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(o => o.CreatedAt)
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(o => o.CreatedByUserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(o => o.PaymentStatus)
            .HasConversion<int>()
            .HasDefaultValue(PaymentStatus.Unpaid)
            .IsRequired();

        builder.Property(o => o.PaidOn)
            .HasColumnType("TIMESTAMP")
            .IsRequired(false);

        builder.Property(o => o.PaidByUserId)
            .HasMaxLength(450)
            .IsRequired(false);

        // Explicit names: Firebird truncates identifiers longer than 31 characters.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .HasConstraintName("FK_OtherObl_Users")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ObligationType>()
            .WithMany()
            .HasForeignKey(o => o.ObligationTypeId)
            .HasConstraintName("FK_OtherObl_OblTypes")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => o.ObligationTypeId)
            .HasDatabaseName("IX_OtherObl_TypeId");

        builder.HasIndex(o => o.UserId)
            .HasDatabaseName("IX_OtherObl_UserId");

        builder.HasIndex(o => o.Date)
            .HasDatabaseName("IX_OtherObl_Date");

        builder.HasIndex(o => o.PaymentStatus)
            .HasDatabaseName("IX_OtherObl_PaymentStatus");
    }
}
