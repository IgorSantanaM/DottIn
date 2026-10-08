using DottIn.Domain.Core.Models;

namespace DottIn.Domain.Payrolls;

public sealed class AccountantBranchAccess : Entity<Guid>
{
    public Guid BranchId { get; private set; }
    public Guid AccountantEmployeeId { get; private set; }
    public Guid GrantedByEmployeeId { get; private set; }
    public DateTime GrantedAt { get; private set; }
    private AccountantBranchAccess() { }
    public AccountantBranchAccess(Guid branchId, Guid accountantId, Guid grantedBy, DateTime now)
    {
        Id = Guid.NewGuid(); BranchId = branchId; AccountantEmployeeId = accountantId;
        GrantedByEmployeeId = grantedBy; GrantedAt = now;
    }
}
