using Domain.Entities;
using Domain.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations;

public class MenuReminderLogConfiguration : IEntityTypeConfiguration<MenuReminderLog>
{
    public void Configure(EntityTypeBuilder<MenuReminderLog> builder)
    {
        builder.ToTable("MenuReminderLogs");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnType("VARCHAR(36)")
            .ValueGeneratedNever();

        builder.Property(r => r.UserId)
            .IsRequired();

        builder.Property<Guid>(r => r.MenuId)
            .HasColumnType("VARCHAR(36)")
            .IsRequired();

        builder.Property(r => r.SentAt)
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        // Explicit names: Firebird truncates identifiers longer than 31 characters.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .HasConstraintName("FK_MenuRemLogs_Users")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Menu>()
            .WithMany()
            .HasForeignKey(r => r.MenuId)
            .HasConstraintName("FK_MenuRemLogs_Menus")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.UserId, r.MenuId })
            .IsUnique()
            .HasDatabaseName("UX_MenuRemLogs_User_Menu");

        builder.HasIndex(r => r.MenuId)
            .HasDatabaseName("IX_MenuRemLogs_MenuId");
    }
}
