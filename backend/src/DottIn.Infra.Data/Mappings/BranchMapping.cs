using DottIn.Domain.Branches;
using DottIn.Infra.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DottIn.Infra.Data.Mappings
{
    public class BranchMapping : EntityTypeConfiguration<Branch>
    {
        public override void Configure(EntityTypeBuilder<Branch> builder)
        {
            builder.ToTable("Branches");

            builder.HasKey(b => b.Id);

            builder.Property(b => b.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.ComplexProperty(b => b.Document, doc =>
            {
                doc.Property(d => d.Value)
                    .IsRequired()
                    .HasMaxLength(14)
                    .HasColumnName("Document");

                doc.Property(d => d.Type)
                    .IsRequired()
                    .HasColumnName("DocumentType");
            });

            builder.HasIndex("Document")
                .IsUnique();

            builder.HasIndex(b => b.IsActive);

            builder.HasIndex(b => b.OwnerId);

            builder.Property(b => b.Email)
                .IsRequired(false)
                .HasMaxLength(255);

            builder.Property(b => b.PhoneNumber)
                .IsRequired(false)
                .HasMaxLength(20);

            builder.ComplexProperty(b => b.Address, addr =>
            {
                addr.Property(a => a.Street)
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnName("Street");

                addr.Property(a => a.Number)
                    .IsRequired()
                    .HasColumnName("Number");

                addr.Property(a => a.Complement)
                    .IsRequired(false)
                    .HasMaxLength(100)
                    .HasColumnName("Complement");

                addr.Property(a => a.City)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("City");

                addr.Property(a => a.State)
                    .IsRequired()
                    .HasMaxLength(2)
                    .IsFixedLength()
                    .HasColumnName("State");

                addr.Property(a => a.ZipCode)
                    .IsRequired()
                    .HasMaxLength(8)
                    .IsFixedLength()
                    .HasColumnName("ZipCode");
            });

            builder.ComplexProperty(b => b.Location, loc =>
            {
                loc.Property(l => l.Latitude)
                    .IsRequired()
                    .HasColumnName("Latitude");

                loc.Property(l => l.Longitude)
                    .IsRequired()
                    .HasColumnName("Longitude");
            });

            builder.Property(b => b.TimeZoneId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(b => b.AllowedRadiusMeters)
                .IsRequired();

            builder.Property(b => b.ToleranceMinutes)
                .IsRequired();

            builder.Property(b => b.HolidayCalendarId)
                .IsRequired(false);

            builder.Property(b => b.AllowOvernightShifts)
                .IsRequired();

            builder.Property(b => b.IsActive)
                .IsRequired();

            builder.Property(b => b.IsHeadquarters)
                .IsRequired();

            builder.Property(b => b.OwnerId)
                .IsRequired(false)
                .HasMaxLength(100);

            builder.Property(b => b.StartWorkTime)
                .IsRequired()
                .HasColumnType("time without time zone");

            builder.Property(b => b.EndWorkTime)
                .IsRequired()
                .HasColumnType("time without time zone");

            builder.Property(b => b.CreatedAt)
                .IsRequired()
                .HasColumnType("timestamp with time zone");

            builder.Property(b => b.UpdatedAt)
                .IsRequired(false)
                .HasColumnType("timestamp with time zone");
        }
    }
}
