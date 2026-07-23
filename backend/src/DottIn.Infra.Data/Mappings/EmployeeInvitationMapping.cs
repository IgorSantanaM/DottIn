using DottIn.Domain.Branches;
using DottIn.Domain.Employees;
using DottIn.Infra.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DottIn.Infra.Data.Mappings;

public sealed class EmployeeInvitationMapping : EntityTypeConfiguration<EmployeeInvitation>
{
    public override void Configure(EntityTypeBuilder<EmployeeInvitation> builder)
    {
        builder.ToTable("EmployeeInvitations", table =>
            table.HasCheckConstraint(
                "CK_EmployeeInvitations_Role",
                "\"Role\" IN ('Employee', 'Manager', 'Administrator')"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TokenHash).IsRequired().HasMaxLength(64).IsFixedLength();
        builder.HasIndex(x => x.TokenHash).IsUnique();

        builder.Property(x => x.Email).HasMaxLength(254);
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.RevokedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.ConsumedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.ConcurrencyToken).IsRequired().IsConcurrencyToken();

        builder.HasIndex(x => new { x.BranchId, x.ExpiresAt });
        builder.HasIndex(x => x.InvitedByEmployeeId);
        builder.HasIndex(x => x.ConsumedByEmployeeId);

        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => new { x.BranchId, x.InvitedByEmployeeId })
            .HasPrincipalKey(employee => new { employee.BranchId, employee.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => x.ConsumedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
