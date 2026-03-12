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
        DominioCode = dominioCode.PadLeft(10, '0');
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateCode(string dominioCode)
    {
        DominioCode = dominioCode.PadLeft(10, '0');
        UpdatedAt = DateTime.UtcNow;
    }
}
