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
            builder.ToTable("TimeKeepings");

            builder.HasKey(tk => tk.Id);

            builder.Property(tk => tk.EmployeeId)
                .IsRequired();

            builder.Property(tk => tk.BranchId)
                .IsRequired();

            builder.Property(tk => tk.WorkDate)
                .IsRequired()
                .HasColumnType("date");

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
        }
    }
}
