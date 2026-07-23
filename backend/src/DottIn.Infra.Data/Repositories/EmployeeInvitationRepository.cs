using DottIn.Domain.Employees;
using DottIn.Infra.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace DottIn.Infra.Data.Repositories;

public sealed class EmployeeInvitationRepository(DottInContext context)
    : Repository<EmployeeInvitation, Guid>(context), IEmployeeInvitationRepository
{
    public Task<EmployeeInvitation?> GetByTokenHashAsync(string tokenHash, CancellationToken token = default)
        => context.EmployeeInvitations.FirstOrDefaultAsync(x => x.TokenHash == tokenHash, token);

    public async Task<IEnumerable<EmployeeInvitation>> GetByBranchAsync(Guid branchId, CancellationToken token = default)
        => await context.EmployeeInvitations
            .AsNoTracking()
            .Where(x => x.BranchId == branchId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(token);

    public async Task<int> CountPendingByOwnerIdAsync(Guid ownerId, DateTime nowUtc, CancellationToken token = default)
    {
        var branchIds = context.Branches
            .Where(x => x.OwnerId == ownerId)
            .Select(x => x.Id);

        return await context.EmployeeInvitations.CountAsync(
            x => branchIds.Contains(x.BranchId) &&
                 x.ConsumedAt == null && x.RevokedAt == null && x.ExpiresAt > nowUtc,
            token);
    }
}
