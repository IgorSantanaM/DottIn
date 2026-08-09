using DottIn.Domain.Core.Models;

namespace DottIn.Domain.Exports;

public class DominioEmployeeMapping : Entity<Guid>, IAggregateRoot
{
    public Guid EmployeeId { get; private set; }
    public Guid BranchId { get; private set; }
    public string DominioCode { get; private set; } = "";
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private DominioEmployeeMapping() { }

    public DominioEmployeeMapping(Guid employeeId, Guid branchId, string dominioCode)
    {
        Id = Guid.NewGuid();
        EmployeeId = employeeId;
        BranchId = branchId;
        DominioCode = NormalizeCode(dominioCode);
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateCode(string dominioCode)
    {
        DominioCode = NormalizeCode(dominioCode);
        UpdatedAt = DateTime.UtcNow;
    }

    private static string NormalizeCode(string dominioCode)
    {
        var code = dominioCode?.Trim() ?? string.Empty;
        if (code.Length == 0 || code.Length > 10 || !code.All(char.IsDigit))
            throw new ArgumentException("O código do empregado no Domínio deve conter de 1 a 10 dígitos numéricos.", nameof(dominioCode));

        return code.PadLeft(10, '0');
    }
}
