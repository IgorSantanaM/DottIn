using DottIn.Domain.Core.Data;

namespace DottIn.Domain.TimeKeepings;

public interface ITimeKeepingAdjustmentRepository : IRepository<TimeKeepingAdjustment, Guid>
{
    Task<IEnumerable<TimeKeepingAdjustment>> GetByBranchAsync(
        Guid branchId,
        TimeKeepingAdjustmentStatus? status = null,
        CancellationToken token = default);

    Task<IEnumerable<TimeKeepingAdjustment>> GetApprovedByTimeKeepingIdsAsync(
        IEnumerable<Guid> timeKeepingIds,
        CancellationToken token = default);

    Task<bool> HasPendingForEntryAsync(
        Guid timeKeepingId,
        TimeKeepingType entryType,
        DateTime? originalTimestamp,
        CancellationToken token = default);
}
