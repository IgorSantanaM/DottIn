using DottIn.Domain.Core.Models;

namespace DottIn.Domain.Payrolls;

public sealed class PayrollPaymentChange : Entity<Guid>
{
    public Guid PayrollId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid ChangedByEmployeeId { get; private set; }
    public decimal? PreviousAmount { get; private set; }
    public decimal NewAmount { get; private set; }
    public DateTime ChangedAt { get; private set; }
    private PayrollPaymentChange() { }
    public PayrollPaymentChange(Guid payrollId, Guid employeeId, Guid actor, decimal? previous, decimal current, DateTime now)
    {
        Id = Guid.NewGuid(); PayrollId = payrollId; EmployeeId = employeeId;
        ChangedByEmployeeId = actor; PreviousAmount = previous; NewAmount = current; ChangedAt = now;
    }
}

public sealed class PayrollExportEvent : Entity<Guid>
{
    public Guid PayrollId { get; private set; }
    public Guid ExportedByEmployeeId { get; private set; }
    public DateTime ExportedAt { get; private set; }
    private PayrollExportEvent() { }
    public PayrollExportEvent(Guid payrollId, Guid actor, DateTime now)
    {
        Id = Guid.NewGuid(); PayrollId = payrollId; ExportedByEmployeeId = actor; ExportedAt = now;
    }
}
