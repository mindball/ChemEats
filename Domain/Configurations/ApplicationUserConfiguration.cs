﻿using Domain.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("AspNetUsers");

        builder.Property(u => u.Abbreviation)
            .HasMaxLength(20); // примерно

        builder.HasMany(u => u.Orders)
            .WithOne(o => o.User)
            .HasForeignKey("UserId") 
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

    }
}