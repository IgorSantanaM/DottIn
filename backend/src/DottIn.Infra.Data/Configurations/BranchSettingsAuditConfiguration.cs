using DottIn.Domain.Branches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DottIn.Infra.Data.Configurations;

public sealed class BranchSettingsAuditConfiguration : IEntityTypeConfiguration<BranchSettingsAudit>
{
    public void Configure(EntityTypeBuilder<BranchSettingsAudit> builder)
    {
        builder.ToTable("BranchSettingsAudits");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Section).HasMaxLength(32).IsRequired();
        builder.Property(x => x.BeforeJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.AfterJson).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => new { x.BranchId, x.CreatedAtUtc });
    }
}
