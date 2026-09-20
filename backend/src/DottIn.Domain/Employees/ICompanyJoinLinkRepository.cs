using DottIn.Domain.Core.Data;

namespace DottIn.Domain.Employees;

public interface ICompanyJoinLinkRepository : IRepository<CompanyJoinLink, Guid>
{
    Task<CompanyJoinLink?> GetByBranchIdAsync(Guid branchId, CancellationToken token = default);
}
