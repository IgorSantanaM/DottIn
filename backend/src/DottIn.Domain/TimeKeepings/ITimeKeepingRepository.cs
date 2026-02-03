using DottIn.Domain.Core.Data;

namespace DottIn.Domain.TimeKeepings
{
    public interface ITimeKeepingRepository : IRepository<TimeKeeping, Guid>
    {
        Task<TimeKeeping?> GetTodayByEmployeeAsync(Guid employeeId, DateOnly workDate, CancellationToken token = default);

        Task<TimeKeeping?> GetActiveByEmployeeAsync(Guid employeeId, CancellationToken token = default);

        Task<IEnumerable<TimeKeeping>> GetByEmployeeAndPeriodAsync(
            Guid employeeId,
            DateOnly startDate,
            DateOnly endDate, CancellationToken token = default);
        Task<IEnumerable<TimeKeeping>> GetByBranchAndDateAsync(Guid branchId, DateOnly workDate, CancellationToken token = default);
        Task<IEnumerable<TimeKeeping>> GetActiveByBranchAsync(Guid branchId, CancellationToken token = default);
        Task<bool> ExistsForEmployeeOnDateAsync(Guid employeeId, DateOnly workDate, CancellationToken token = default);
    }
}