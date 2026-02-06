using DottIn.Domain.HolidayCalendars;
using DottIn.Infra.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DottIn.Infra.Data.Mappings
{
    public class HolidayCalendarMapping : EntityTypeConfiguration<HolidayCalendar>
    {
        public override void Configure(EntityTypeBuilder<HolidayCalendar> builder)
        {
            builder.ToTable("HolidayCalendars");

            builder.HasKey(hc => hc.Id);

            builder.Property(hc => hc.BranchId)
                .IsRequired();

            builder.Property(hc => hc.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(hc => hc.Description)
                .IsRequired(false)
                .HasMaxLength(500);

            builder.Property(hc => hc.CountryCode)
                .IsRequired()
                .HasMaxLength(2)
                .IsFixedLength();

            builder.Property(hc => hc.RegionCode)
                .IsRequired(false)
                .HasMaxLength(10);

            builder.Property(hc => hc.Year)
                .IsRequired();

            builder.Property(hc => hc.IsActive)
                .IsRequired();

            builder.Property(hc => hc.CreatedAt)
                .IsRequired()
                .HasColumnType("timestamp with time zone");

            builder.Property(hc => hc.UpdatedAt)
                .IsRequired(false)
                .HasColumnType("timestamp with time zone");

            builder.OwnsMany(hc => hc.Holidays, holiday =>
            {
                holiday.ToTable("Holidays");

                holiday.WithOwner().HasForeignKey("HolidayCalendarId");

                holiday.Property<int>("Id")
                    .ValueGeneratedOnAdd();

                holiday.HasKey("Id");

                holiday.Property(h => h.Date)
                    .IsRequired()
                    .HasColumnType("date");

                holiday.Property(h => h.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                holiday.Property(h => h.Type)
                    .IsRequired()
                    .HasConversion<string>()
                    .HasMaxLength(20);

                holiday.Property(h => h.IsOptional)
                    .IsRequired()
                    .HasDefaultValue(false);

                holiday.HasIndex("HolidayCalendarId", nameof(Holiday.Date))
                    .IsUnique();

                holiday.HasIndex(h => h.Date);

                holiday.HasIndex(h => h.Type);

                holiday.HasIndex(h => h.IsOptional);
            });

            builder.HasIndex(hc => new { hc.BranchId, hc.Year, hc.IsActive })
                .HasFilter("\"IsActive\" = true")
                .IsUnique();

            builder.HasIndex(hc => hc.BranchId);

            builder.HasIndex(hc => new { hc.BranchId, hc.Year });

            builder.HasIndex(hc => new { hc.CountryCode, hc.RegionCode, hc.Year });

            builder.HasIndex(hc => hc.IsActive);

            builder.HasIndex(hc => hc.Year);
        }
    }
}
