using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Core.Models;

namespace DottIn.Domain.Payrolls;

public sealed class PayrollItem : Entity<Guid>
{
    public Guid PayrollId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string EmployeeName { get; private set; } = "";
    public string? DominioCode { get; private set; }
    public long WorkedMinutes { get; private set; }
    public bool HasIncompleteRecords { get; private set; }
    public decimal? CalculatedAmount { get; private set; }
    public decimal? PaymentAmount { get; private set; }
    public string? Notes { get; private set; }

    private PayrollItem() { }
    public PayrollItem(Guid payrollId, Guid employeeId, string name, string? dominioCode, long minutes, bool incomplete = false)
    {
        if (payrollId == Guid.Empty || employeeId == Guid.Empty || string.IsNullOrWhiteSpace(name) || minutes < 0)
            throw new DomainException("Colaborador ou horas inválidos.");
        Id = Guid.NewGuid();
        PayrollId = payrollId;
        EmployeeId = employeeId;
        SetSnapshot(name, dominioCode, minutes, incomplete);
    }

    public void SetSnapshot(string name, string? dominioCode, long minutes, bool incomplete = false)
    {
        if (string.IsNullOrWhiteSpace(name) || minutes < 0)
            throw new DomainException("Colaborador ou horas inválidos.");
        EmployeeName = name.Trim();
        DominioCode = string.IsNullOrWhiteSpace(dominioCode) ? null : dominioCode.Trim();
        WorkedMinutes = minutes;
        HasIncompleteRecords = incomplete;
    }

    public void SetPayment(decimal amount, string? notes)
    {
        if (amount < 0 || amount > 999999999.99m || decimal.Round(amount, 2) != amount)
            throw new DomainException("O valor da folha deve ser positivo e ter no máximo duas casas decimais.");
        if (notes?.Length > 500)
            throw new DomainException("A observação deve ter até 500 caracteres.");
        PaymentAmount = amount;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }
}
