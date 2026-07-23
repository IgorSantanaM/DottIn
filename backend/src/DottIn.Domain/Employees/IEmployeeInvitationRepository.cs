using DottIn.Domain.Core.Data;

namespace DottIn.Domain.Employees;

public interface IEmployeeInvitationRepository : IRepository<EmployeeInvitation, Guid>
{
    Task<EmployeeInvitation?> GetByTokenHashAsync(string tokenHash, CancellationToken token = default);
    Task<IEnumerable<EmployeeInvitation>> GetByBranchAsync(Guid branchId, CancellationToken token = default);
    Task<int> CountPendingByOwnerIdAsync(Guid ownerId, DateTime nowUtc, CancellationToken token = default);
}
