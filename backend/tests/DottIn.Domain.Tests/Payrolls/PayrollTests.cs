using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Payrolls;

namespace DottIn.Domain.Tests.Payrolls;

public sealed class PayrollTests
{
    private static readonly DateTime Now = new(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ClosedPayroll_UsesApprovedAmountAndRejectsFurtherChanges()
    {
        var owner = Guid.NewGuid();
        var payroll = new Payroll(Guid.NewGuid(), 2026, 10, owner, Now);
        var item = new PayrollItem(payroll.Id, Guid.NewGuid(), "João da Silva", "0000012345", 10110);
        item.SetPayment(3500.00m, "Conferido");

        payroll.Close(owner, Now.AddMinutes(1), [item]);

        Assert.Equal(PayrollStatus.ReadyForAccounting, payroll.Status);
        Assert.Equal(3500.00m, item.PaymentAmount);
        Assert.Throws<DomainException>(payroll.EnsureDraft);
        payroll.MarkExported(Guid.NewGuid(), Now.AddMinutes(2));
        Assert.Equal(PayrollStatus.Exported, payroll.Status);
    }

    [Fact]
    public void IncompleteOrUnmappedPayroll_CannotClose()
    {
        var owner = Guid.NewGuid();
        var payroll = new Payroll(Guid.NewGuid(), 2026, 10, owner, Now);
        var item = new PayrollItem(payroll.Id, Guid.NewGuid(), "Maria", null, 60, incomplete: true);
        item.SetPayment(100m, null);
        Assert.Throws<DomainException>(() => payroll.Close(owner, Now, [item]));

        item.SetSnapshot("Maria", null, 60);
        var unmapped = Assert.Throws<DomainException>(() => payroll.Close(owner, Now, [item]));
        Assert.Contains("Maria", unmapped.Message);

        item.SetSnapshot("Maria", "0000000001", 60, incomplete: true);
        Assert.Throws<DomainException>(() => payroll.Close(owner, Now, [item]));
        item.SetSnapshot("Maria", "0000000001", 60);
        payroll.Close(owner, Now, [item]);
    }

    [Fact]
    public void InvalidPayment_IsRejected()
    {
        var item = new PayrollItem(Guid.NewGuid(), Guid.NewGuid(), "Maria", "123", 0);
        Assert.Throws<DomainException>(() => item.SetPayment(-1m, null));
        Assert.Throws<DomainException>(() => item.SetPayment(12.345m, null));
    }

    [Fact]
    public void Competence_IsPerBranchAndMonth()
    {
        var branch = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var october = new Payroll(branch, 2026, 10, owner, Now);
        var november = new Payroll(branch, 2026, 11, owner, Now);
        Assert.NotEqual(october.Id, november.Id);
        Assert.Equal(branch, october.BranchId);
        Assert.Equal(10, october.Month);
        Assert.Equal(11, november.Month);
    }
}
