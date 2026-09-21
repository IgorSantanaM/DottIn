using DottIn.Domain.TimeKeepings;
using DottIn.Infra.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace DottIn.Infra.Data.Repositories;

public sealed class TimeKeepingAdjustmentRepository(DottInContext context)
    : Repository<TimeKeepingAdjustment, Guid>(context), ITimeKeepingAdjustmentRepository
{
    public async Task<IEnumerable<TimeKeepingAdjustment>> GetByBranchAsync(
        Guid branchId,
        TimeKeepingAdjustmentStatus? status = null,
        CancellationToken token = default)
    {
        var query = context.TimeKeepingAdjustments.AsNoTracking().Where(x => x.BranchId == branchId);
        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        return await query.OrderByDescending(x => x.CreatedAt).ToListAsync(token);
    }

    public async Task<IEnumerable<TimeKeepingAdjustment>> GetApprovedByTimeKeepingIdsAsync(
        IEnumerable<Guid> timeKeepingIds,
        CancellationToken token = default)
    {
        var ids = timeKeepingIds.Distinct().ToArray();
        if (ids.Length == 0)
            return [];

        return await context.TimeKeepingAdjustments
            .AsNoTracking()
            .Where(x => ids.Contains(x.TimeKeepingId) && x.Status == TimeKeepingAdjustmentStatus.Approved)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(token);
    }

    public Task<bool> HasPendingForEntryAsync(
        Guid timeKeepingId,
        TimeKeepingType entryType,
        DateTime? originalTimestamp,
        CancellationToken token = default)
        => context.TimeKeepingAdjustments.AnyAsync(
            x => x.TimeKeepingId == timeKeepingId &&
                 x.EntryType == entryType &&
                 x.OriginalTimestamp == originalTimestamp &&
                 x.Status == TimeKeepingAdjustmentStatus.Pending,
            token);
}
