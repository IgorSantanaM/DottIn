using DottIn.Domain.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DottIn.Infra.Data.Mappings
{
    public class RefreshTokenMapping : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.EmployeeId)
                .IsRequired();

            builder.Property(r => r.BranchId)
                .IsRequired();

            builder.Property(r => r.Token)
                .IsRequired()
                .HasMaxLength(128);

            builder.Property(r => r.ExpiresAt)
                .IsRequired();

            builder.Property(r => r.CreatedAt)
                .IsRequired();

            builder.Property(r => r.RevokedAt);

            builder.HasIndex(r => r.Token)
                .IsUnique();

            builder.HasIndex(r => r.EmployeeId);
        }
    }
}
