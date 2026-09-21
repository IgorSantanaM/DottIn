using DottIn.Domain.Branches;
using DottIn.Domain.Employees;
using DottIn.Domain.TimeKeepings;
using DottIn.Infra.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DottIn.Infra.Data.Mappings;

public sealed class TimeKeepingAdjustmentMapping : EntityTypeConfiguration<TimeKeepingAdjustment>
{
    public override void Configure(EntityTypeBuilder<TimeKeepingAdjustment> builder)
    {
        builder.ToTable("TimeKeepingAdjustments", table =>
        {
            table.HasCheckConstraint(
                "CK_TimeKeepingAdjustments_Status",
                "\"Status\" IN ('Pending', 'Approved', 'Rejected')");
            table.HasCheckConstraint(
                "CK_TimeKeepingAdjustments_EntryType",
                "\"EntryType\" IN ('ClockIn', 'BreakStart', 'BreakEnd', 'ClockOut')");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntryType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.OriginalTimestamp).HasColumnType("timestamp with time zone");
        builder.Property(x => x.ProposedTimestamp).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ReviewedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ReviewNote).HasMaxLength(500);
        builder.Property(x => x.ConcurrencyToken).IsRequired().IsConcurrencyToken();

        builder.HasIndex(x => new { x.BranchId, x.Status, x.CreatedAt });
        builder.HasIndex(x => x.TimeKeepingId);
        builder.HasIndex(x => new { x.EmployeeId, x.CreatedAt });

        builder.HasOne<TimeKeeping>()
            .WithMany()
            .HasForeignKey(x => x.TimeKeepingId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => x.RequestedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => x.ReviewedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
