using DottIn.Domain.Subscriptions;
using DottIn.Infra.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DottIn.Infra.Data.Mappings;

public sealed class StripeWebhookReceiptMapping : EntityTypeConfiguration<StripeWebhookReceipt>
{
    public override void Configure(EntityTypeBuilder<StripeWebhookReceipt> builder)
    {
        builder.ToTable("StripeWebhookReceipts", table =>
            table.HasCheckConstraint(
                "CK_StripeWebhookReceipts_Status",
                "\"Status\" IN ('Processing', 'Processed', 'Failed')"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventId)
            .IsRequired()
            .HasMaxLength(255);
        builder.HasIndex(x => x.EventId).IsUnique();

        builder.Property(x => x.EventType)
            .IsRequired()
            .HasMaxLength(100);
        builder.HasIndex(x => x.EventType);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.HasIndex(x => x.Status);

        builder.Property(x => x.AttemptCount).IsRequired();
        builder.Property(x => x.ReceivedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ProcessedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.LastError).HasMaxLength(1000);
    }
}
