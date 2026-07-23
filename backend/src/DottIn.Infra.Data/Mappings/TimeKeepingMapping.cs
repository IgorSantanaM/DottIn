using DottIn.Domain.Branches;
using DottIn.Domain.Employees;
using DottIn.Domain.TimeKeepings;
using DottIn.Infra.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DottIn.Infra.Data.Mappings
{
    public class TimeKeepingMapping : EntityTypeConfiguration<TimeKeeping>
    {
        public override void Configure(EntityTypeBuilder<TimeKeeping> builder)
        {
            builder.ToTable("TimeKeepings", table =>
            {
                table.HasCheckConstraint(
                    "CK_TimeKeepings_Source",
                    "\"Source\" IN ('Mobile', 'Web', 'Kiosk')");
                table.HasCheckConstraint(
                    "CK_TimeKeepings_TimeZoneId",
                    "length(trim(\"TimeZoneId\")) > 0");
            });

            builder.HasKey(tk => tk.Id);

            builder.Property(tk => tk.EmployeeId)
                .IsRequired();

            builder.Property(tk => tk.BranchId)
                .IsRequired();

            builder.Property(tk => tk.WorkDate)
                .IsRequired()
                .HasColumnType("date");

            builder.Property(tk => tk.TimeZoneId)
                .IsRequired()
                .HasMaxLength(100)
                .HasDefaultValue("UTC");

            builder.Property(tk => tk.CreatedAt)
                .IsRequired()
                .HasColumnType("timestamp with time zone");

            builder.ComplexProperty(tk => tk.Location, loc =>
            {
                loc.Property(l => l.Latitude)
                    .HasColumnName("Latitude");

                loc.Property(l => l.Longitude)
                    .HasColumnName("Longitude");
            });

            builder.Ignore(tk => tk.Status);

            builder.Property(tk => tk.Source)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(ClockSource.Mobile);

            builder.Property(tk => tk.ConcurrencyToken)
                .IsRequired()
                .IsConcurrencyToken();

            builder.OwnsMany(tk => tk.Entries, entry =>
            {
                entry.ToTable("TimeEntries");

                entry.WithOwner().HasForeignKey("TimeKeepingId");

                entry.Property<int>("Id")
                    .ValueGeneratedOnAdd();

                entry.HasKey("Id");

                entry.Property(e => e.Timestamp)
                    .IsRequired()
                    .HasColumnType("timestamp with time zone");

                entry.Property(e => e.Type)
                    .IsRequired()
                    .HasConversion<string>()
                    .HasMaxLength(20);

                entry.HasIndex("TimeKeepingId", nameof(TimeEntry.Timestamp));

                entry.HasIndex("TimeKeepingId", nameof(TimeEntry.Type));
            });

            builder.HasIndex(tk => new { tk.EmployeeId, tk.WorkDate })
                .IsUnique();

            builder.HasIndex(tk => new { tk.BranchId, tk.WorkDate });

            builder.HasIndex(tk => tk.EmployeeId);

            builder.HasIndex(tk => tk.BranchId);

            builder.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(tk => tk.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(tk => new { tk.BranchId, tk.EmployeeId })
                .HasPrincipalKey(employee => new { employee.BranchId, employee.Id })
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
