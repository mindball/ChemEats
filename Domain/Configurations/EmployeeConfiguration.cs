using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations;

// public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
// {
//     public void Configure(EntityTypeBuilder<Employee> builder)
//     {
//         builder.HasKey(e => e.Id);
//
//         builder.Property(e => e.Id)
//             .HasConversion(id => id.Value, value => new EmployeeId(value))
//             .ValueGeneratedNever();
//
//         builder.Property(e => e.FullName)
//             .IsRequired()
//             .HasMaxLength(100);
//
//         builder.Property(e => e.Abbreviation)
//             .IsRequired()
//             .HasMaxLength(10);
//
//         builder.Metadata.FindNavigation(nameof(Employee.Orders))!
//             .SetPropertyAccessMode(PropertyAccessMode.Field);
//     }
// }

