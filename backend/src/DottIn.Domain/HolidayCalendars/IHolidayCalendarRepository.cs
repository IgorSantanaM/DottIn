using DottIn.Domain.Core.Data;

namespace DottIn.Domain.HolidayCalendars
{
    public interface IHolidayCalendarRepository : IRepository<HolidayCalendar, Guid>
    {
        Task<HolidayCalendar?> GetByBranchAndYearAsync(
            Guid branchId,
            int year,
            CancellationToken token = default);

        Task<IEnumerable<HolidayCalendar>> GetByBranchAsync(
            Guid branchId,
            CancellationToken token = default);

        Task<IEnumerable<HolidayCalendar>> GetActiveByBranchAsync(
            Guid branchId,
            CancellationToken token = default);

        Task<HolidayCalendar?> GetByCountryAndYearAsync(
            string countryCode,
            int year,
            string? regionCode = null,
            CancellationToken token = default);

        Task<IEnumerable<HolidayCalendar>> GetByYearAsync(
            int year,
            CancellationToken token = default);

        Task<bool> IsHolidayForBranchAsync(
            Guid branchId,
            DateOnly date,
            CancellationToken token = default);

        Task<bool> IsMandatoryHolidayForBranchAsync(
            Guid branchId,
            DateOnly date,
            CancellationToken token = default);

        Task<Holiday?> GetHolidayByBranchAndDateAsync(
            Guid branchId,
            DateOnly date,
            CancellationToken token = default);

        Task<IEnumerable<Holiday>> GetHolidaysInRangeForBranchAsync(
            Guid branchId,
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken token = default);

        Task<HolidayCalendar?> GetWithHolidaysAsync(
            Guid id,
            CancellationToken token = default);

        Task<HolidayCalendar?> GetForUpdateAsync(
            Guid id,
            CancellationToken token = default);

        Task<bool> ExistsForBranchAndYearAsync(
            Guid branchId,
            int year,
            CancellationToken token = default);
    }
}
