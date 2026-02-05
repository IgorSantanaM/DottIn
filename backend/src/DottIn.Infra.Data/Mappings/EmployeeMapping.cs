using DottIn.Domain.Employees;
using DottIn.Infra.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DottIn.Infra.Data.Mappings
{
    public class EmployeeMapping : EntityTypeConfiguration<Employee>
    {
        public override void Configure(EntityTypeBuilder<Employee> builder)
        {
            builder.ToTable("Employees");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.ComplexProperty(e => e.CPF, cpf =>
            {
                cpf.Property(c => c.Value)
                    .IsRequired()
                    .HasMaxLength(11)
                    .IsFixedLength()
                    .HasColumnName("CPF");

                cpf.Property(c => c.Type)
                    .IsRequired()
                    .HasColumnName("DocumentType");
            });

            builder.HasIndex("CPF")
                .IsUnique();

            builder.Property(e => e.ImageUrl)
                .HasMaxLength(500);

            builder.Property(e => e.BranchId)
                .IsRequired();

            builder.Property(e => e.StartWorkTime)
                .IsRequired()
                .HasColumnType("time without time zone");

            builder.Property(e => e.EndWorkTime)
                .IsRequired()
                .HasColumnType("time without time zone");

            builder.Property(e => e.IntervalStart)
                .IsRequired()
                .HasColumnType("time without time zone");

            builder.Property(e => e.IntervalEnd)
                .IsRequired()
                .HasColumnType("time without time zone");

            builder.Property(e => e.CreatedAt)
                .IsRequired()
                .HasColumnType("timestamp with time zone");

            builder.Property(e => e.UpdatedAt)
                .IsRequired(false)
                .HasColumnType("timestamp with time zone");

            builder.Property(e => e.IsActive)
                .IsRequired();

            builder.Property(e => e.AllowOvernightShifts)
                .IsRequired();

            builder.HasIndex(e => e.BranchId);

            builder.HasIndex(e => new { e.BranchId, e.IsActive });

            builder.HasIndex(e => e.IsActive);
        }
    }
}
