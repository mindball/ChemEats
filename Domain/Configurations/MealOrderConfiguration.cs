using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations;

public class MealOrderConfiguration : IEntityTypeConfiguration<MealOrder>
{
    public void Configure(EntityTypeBuilder<MealOrder> builder)
    {
        builder.HasKey(mo => mo.Id);

        builder.Property(m => m.Id)
            .HasColumnType("VARCHAR(36)")
            .ValueGeneratedNever();

        // builder.Property(mo => mo.MealId)
        //     .IsRequired();

        builder.Property<Guid>(mo => mo.MealId)
            .HasColumnType("VARCHAR(36)")
            .IsRequired();

        builder.Property(mo => mo.Date)
            .IsRequired();

        builder.Property(mo => mo.RegisterDate)
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

        builder.Property(x => x.PaymentStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.PaidOn)
            .IsRequired(false);

        builder.Property(x => x.PaymentStatus)
            .HasDefaultValue(PaymentStatus.Unpaid);

        builder.HasIndex(x => x.PaymentStatus);
    }
}