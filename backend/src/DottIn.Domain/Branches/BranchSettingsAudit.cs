namespace DottIn.Domain.Branches;

/// <summary>Administrative changes only; never contains credentials or tokens.</summary>
public sealed class BranchSettingsAudit
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid BranchId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string Section { get; private set; } = "";
    public string BeforeJson { get; private set; } = "";
    public string AfterJson { get; private set; } = "";
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    private BranchSettingsAudit() { }
    public BranchSettingsAudit(Guid branchId, Guid employeeId, string section, string beforeJson, string afterJson)
        => (BranchId, EmployeeId, Section, BeforeJson, AfterJson) =
            (branchId, employeeId, section, beforeJson, afterJson);
}
