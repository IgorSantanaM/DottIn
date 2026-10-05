using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Core.Models;

namespace DottIn.Domain.Payrolls;

public enum PayrollStatus { Draft, ReadyForAccounting, Exported }

public sealed class Payroll : Entity<Guid>, IAggregateRoot
{
    public Guid BranchId { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public PayrollStatus Status { get; private set; }
    public Guid CreatedByEmployeeId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid? ClosedByEmployeeId { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public Guid? ExportedByEmployeeId { get; private set; }
    public DateTime? ExportedAt { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

    private Payroll() { }

    public Payroll(Guid branchId, int year, int month, Guid actor, DateTime now)
    {
        if (branchId == Guid.Empty || actor == Guid.Empty || year is < 2000 or > 2100 || month is < 1 or > 12)
            throw new DomainException("Filial, competência ou responsável inválido.");
        Id = Guid.NewGuid();
        BranchId = branchId;
        Year = year;
        Month = month;
        CreatedByEmployeeId = actor;
        CreatedAt = now;
        Status = PayrollStatus.Draft;
        ConcurrencyToken = Guid.NewGuid();
    }

    public void EnsureDraft()
    {
        if (Status != PayrollStatus.Draft)
            throw new DomainException("A folha já foi finalizada e não pode ser alterada.");
    }

    public void Touch()
    {
        EnsureDraft();
        ConcurrencyToken = Guid.NewGuid();
    }

    public void Close(Guid actor, DateTime now, IReadOnlyCollection<PayrollItem> items)
    {
        EnsureDraft();
        if (items.Count == 0)
            throw new DomainException("A folha não possui colaboradores para finalizar.");
        if (items.Any(x => x.HasIncompleteRecords))
            throw new DomainException("Corrija as jornadas incompletas antes de finalizar a folha.");
        var unmapped = items.Where(x => string.IsNullOrWhiteSpace(x.DominioCode))
            .Select(x => x.EmployeeName).OrderBy(x => x).ToArray();
        if (unmapped.Length > 0)
            throw new DomainException($"Informe o código Domínio de: {string.Join(", ", unmapped)}.");
        if (items.Any(x => x.PaymentAmount is null || string.IsNullOrWhiteSpace(x.EmployeeName)))
            throw new DomainException("Preencha o nome e o valor de todos os colaboradores antes de finalizar.");
        Status = PayrollStatus.ReadyForAccounting;
        ClosedByEmployeeId = actor;
        ClosedAt = now;
        ConcurrencyToken = Guid.NewGuid();
    }

    public void MarkExported(Guid actor, DateTime now)
    {
        if (Status is not (PayrollStatus.ReadyForAccounting or PayrollStatus.Exported))
            throw new DomainException("Finalize a folha antes de exportar.");
        Status = PayrollStatus.Exported;
        ExportedByEmployeeId = actor;
        ExportedAt = now;
        ConcurrencyToken = Guid.NewGuid();
    }
}
