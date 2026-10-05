using DottIn.Domain.Branches;
using DottIn.Domain.Employees;
using DottIn.Domain.Payrolls;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DottIn.Infra.Data.Mappings;

public sealed class PayrollMapping : IEntityTypeConfiguration<Payroll>
{
    public void Configure(EntityTypeBuilder<Payroll> b)
    {
        b.ToTable("Payrolls"); b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.BranchId, x.Year, x.Month }).IsUnique();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        b.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
        b.Property(x => x.ClosedAt).HasColumnType("timestamp with time zone");
        b.Property(x => x.ExportedAt).HasColumnType("timestamp with time zone");
        b.Property(x => x.ConcurrencyToken).IsConcurrencyToken();
        b.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Employee>().WithMany().HasForeignKey(x => x.CreatedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PayrollItemMapping : IEntityTypeConfiguration<PayrollItem>
{
    public void Configure(EntityTypeBuilder<PayrollItem> b)
    {
        b.ToTable("PayrollItems"); b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.PayrollId, x.EmployeeId }).IsUnique();
        b.Property(x => x.EmployeeName).HasMaxLength(150).IsRequired();
        b.Property(x => x.DominioCode).HasMaxLength(10);
        b.Property(x => x.CalculatedAmount).HasPrecision(18, 2);
        b.Property(x => x.PaymentAmount).HasPrecision(18, 2);
        b.Property(x => x.Notes).HasMaxLength(500);
        b.HasOne<Payroll>().WithMany().HasForeignKey(x => x.PayrollId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AccountantBranchAccessMapping : IEntityTypeConfiguration<AccountantBranchAccess>
{
    public void Configure(EntityTypeBuilder<AccountantBranchAccess> b)
    {
        b.ToTable("AccountantBranchAccesses"); b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.BranchId, x.AccountantEmployeeId }).IsUnique();
        b.Property(x => x.GrantedAt).HasColumnType("timestamp with time zone");
        b.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Employee>().WithMany().HasForeignKey(x => x.AccountantEmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PayrollPaymentChangeMapping : IEntityTypeConfiguration<PayrollPaymentChange>
{
    public void Configure(EntityTypeBuilder<PayrollPaymentChange> b)
    {
        b.ToTable("PayrollPaymentChanges"); b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.PayrollId, x.ChangedAt });
        b.Property(x => x.PreviousAmount).HasPrecision(18, 2);
        b.Property(x => x.NewAmount).HasPrecision(18, 2);
        b.Property(x => x.ChangedAt).HasColumnType("timestamp with time zone");
        b.HasOne<Payroll>().WithMany().HasForeignKey(x => x.PayrollId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PayrollExportEventMapping : IEntityTypeConfiguration<PayrollExportEvent>
{
    public void Configure(EntityTypeBuilder<PayrollExportEvent> b)
    {
        b.ToTable("PayrollExportEvents"); b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.PayrollId, x.ExportedAt });
        b.Property(x => x.ExportedAt).HasColumnType("timestamp with time zone");
        b.HasOne<Payroll>().WithMany().HasForeignKey(x => x.PayrollId).OnDelete(DeleteBehavior.Cascade);
    }
}
