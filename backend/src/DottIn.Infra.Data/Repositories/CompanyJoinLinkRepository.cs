using DottIn.Domain.Employees;
using DottIn.Infra.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace DottIn.Infra.Data.Repositories;

public sealed class CompanyJoinLinkRepository(DottInContext context)
    : Repository<CompanyJoinLink, Guid>(context), ICompanyJoinLinkRepository
{
    public Task<CompanyJoinLink?> GetByBranchIdAsync(Guid branchId, CancellationToken token = default)
        => context.CompanyJoinLinks.SingleOrDefaultAsync(x => x.BranchId == branchId, token);
}
