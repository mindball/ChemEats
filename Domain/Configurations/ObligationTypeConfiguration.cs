using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations;

public class ObligationTypeConfiguration : IEntityTypeConfiguration<ObligationType>
{
    public void Configure(EntityTypeBuilder<ObligationType> builder)
    {
        builder.ToTable("ObligationTypes");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnType("VARCHAR(36)")
            .ValueGeneratedNever();

        builder.Property(t => t.Name)
            .HasMaxLength(ObligationType.NameMaxLength)
            .IsRequired();

        builder.HasIndex(t => t.IsDeleted)
            .HasDatabaseName("IX_OblTypes_IsDeleted");
    }
}
