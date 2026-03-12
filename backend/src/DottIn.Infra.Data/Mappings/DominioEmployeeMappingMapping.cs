using DottIn.Domain.Exports;
using DottIn.Infra.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DottIn.Infra.Data.Mappings;

public class DominioEmployeeMappingMapping : EntityTypeConfiguration<DominioEmployeeMapping>
{
    public override void Configure(EntityTypeBuilder<DominioEmployeeMapping> builder)
    {
        builder.ToTable("DominioEmployeeMappings");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.EmployeeId)
            .IsRequired();

        builder.Property(d => d.BranchId)
            .IsRequired();

        builder.Property(d => d.DominioCode)
            .IsRequired()
            .HasMaxLength(10)
            .IsFixedLength();

        builder.Property(d => d.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(d => d.UpdatedAt)
            .IsRequired(false)
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(d => new { d.BranchId, d.EmployeeId })
            .IsUnique();
    }
}
