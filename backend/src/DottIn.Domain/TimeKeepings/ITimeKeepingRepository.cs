using DottIn.Domain.Core.Data;

namespace DottIn.Domain.TimeKeepings
{
    public interface ITimeKeepingRepository : IRepository<TimeKeeping, Guid>
    {
        /// <summary>
        /// Get today's TimeKeeping for read-only operations (AsNoTracking)
        /// </summary>
        Task<TimeKeeping?> GetTodayByEmployeeAsync(Guid employeeId, DateOnly workDate, CancellationToken token = default);

        /// <summary>
        /// Get today's TimeKeeping with change tracking enabled for updates
        /// </summary>
        Task<TimeKeeping?> GetTodayByEmployeeForUpdateAsync(Guid employeeId, DateOnly workDate, CancellationToken token = default);

        Task<TimeKeeping?> GetActiveByEmployeeAsync(Guid employeeId, CancellationToken token = default);

        Task<IEnumerable<TimeKeeping>> GetByEmployeeAndPeriodAsync(
            Guid employeeId,
            DateOnly startDate,
            DateOnly endDate, 
            CancellationToken token = default);

        Task<IEnumerable<TimeKeeping>> GetByBranchAndDateAsync(Guid branchId, DateOnly workDate, CancellationToken token = default);

        Task<IEnumerable<TimeKeeping>> GetActiveByBranchAsync(Guid branchId, CancellationToken token = default);

        Task<bool> ExistsForEmployeeOnDateAsync(Guid employeeId, DateOnly workDate, CancellationToken token = default);
    }
}