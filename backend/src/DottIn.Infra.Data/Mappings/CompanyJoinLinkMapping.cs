using DottIn.Domain.Branches;
using DottIn.Domain.Employees;
using DottIn.Infra.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DottIn.Infra.Data.Mappings;

public sealed class CompanyJoinLinkMapping : EntityTypeConfiguration<CompanyJoinLink>
{
    public override void Configure(EntityTypeBuilder<CompanyJoinLink> builder)
    {
        builder.ToTable("CompanyJoinLinks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.RevokedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.ConcurrencyToken).IsConcurrencyToken().IsRequired();
        builder.HasIndex(x => x.BranchId).IsUnique();
        builder.HasIndex(x => x.ExpiresAt);
        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.CreatedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
