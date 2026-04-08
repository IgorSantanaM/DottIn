using DottIn.Domain.Subscriptions;
using DottIn.Infra.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DottIn.Infra.Data.Mappings
{
    public class SubscriptionPlanMapping : EntityTypeConfiguration<SubscriptionPlan>
    {
        public override void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
        {
            builder.ToTable("SubscriptionPlans");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(p => p.Name)
                .IsUnique();

            builder.Property(p => p.StripePriceId)
                .IsRequired(false)
                .HasMaxLength(100);

            builder.HasIndex(p => p.StripePriceId)
                .IsUnique()
                .HasFilter("\"StripePriceId\" IS NOT NULL");

            builder.Property(p => p.MaxEmployees)
                .IsRequired();

            builder.Property(p => p.MaxBranches)
                .IsRequired();

            builder.Property(p => p.MonthlyPriceBRL)
                .IsRequired()
                .HasPrecision(10, 2);

            builder.Property(p => p.FeaturesJson)
                .IsRequired(false)
                .HasColumnType("jsonb");

            builder.Property(p => p.IsActive)
                .IsRequired();

            builder.HasIndex(p => p.IsActive);

            builder.Property(p => p.CreatedAt)
                .IsRequired()
                .HasColumnType("timestamp with time zone");

            builder.Property(p => p.UpdatedAt)
                .IsRequired(false)
                .HasColumnType("timestamp with time zone");
        }
    }
}
