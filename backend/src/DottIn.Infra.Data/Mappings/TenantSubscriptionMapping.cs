using DottIn.Domain.Subscriptions;
using DottIn.Infra.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DottIn.Infra.Data.Mappings
{
    public class TenantSubscriptionMapping : EntityTypeConfiguration<TenantSubscription>
    {
        public override void Configure(EntityTypeBuilder<TenantSubscription> builder)
        {
            builder.ToTable("TenantSubscriptions");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.HeadquartersId)
                .IsRequired();

            builder.HasIndex(s => s.HeadquartersId)
                .IsUnique();

            builder.Property(s => s.OwnerId)
                .IsRequired();

            builder.HasIndex(s => s.OwnerId)
                .IsUnique();

            builder.Property(s => s.StripeCustomerId)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(s => s.StripeCustomerId)
                .IsUnique();

            builder.Property(s => s.StripeSubscriptionId)
                .IsRequired(false)
                .HasMaxLength(100);

            builder.HasIndex(s => s.StripeSubscriptionId)
                .IsUnique()
                .HasFilter("\"StripeSubscriptionId\" IS NOT NULL");

            builder.Property(s => s.SubscriptionPlanId)
                .IsRequired();

            builder.HasIndex(s => s.SubscriptionPlanId);

            builder.Property(s => s.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.HasIndex(s => s.Status);

            builder.Property(s => s.CurrentPeriodStart)
                .IsRequired()
                .HasColumnType("timestamp with time zone");

            builder.Property(s => s.CurrentPeriodEnd)
                .IsRequired()
                .HasColumnType("timestamp with time zone");

            builder.Property(s => s.CanceledAt)
                .IsRequired(false)
                .HasColumnType("timestamp with time zone");

            builder.Property(s => s.CreatedAt)
                .IsRequired()
                .HasColumnType("timestamp with time zone");

            builder.Property(s => s.UpdatedAt)
                .IsRequired(false)
                .HasColumnType("timestamp with time zone");

            builder.HasOne(s => s.Plan)
                .WithMany()
                .HasForeignKey(s => s.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
