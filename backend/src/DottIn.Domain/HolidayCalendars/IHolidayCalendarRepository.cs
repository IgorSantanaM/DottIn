using DottIn.Domain.Core.Data;

namespace DottIn.Domain.HolidayCalendars
{
    public interface IHolidayCalendarRepository : IRepository<HolidayCalendar, Guid>
    {
        Task<HolidayCalendar?> GetByYearAsync(
            Guid branchId,
            int year,
            CancellationToken token = default);

        Task<IEnumerable<HolidayCalendar>> GetAllAsync(
            Guid branchId,
            CancellationToken token = default);

        Task<IEnumerable<HolidayCalendar>> GetActiveAsync(
            Guid branchId,
            CancellationToken token = default);

        Task<HolidayCalendar?> GetByCountryAndYearAsync(
            Guid branchId,
            string countryCode,
            int year,
            string? regionCode = null,
            CancellationToken token = default);

        Task<bool> IsHolidayAsync(
            Guid branchId,
            DateOnly date,
            CancellationToken token = default);

        Task<bool> IsMandatoryHolidayAsync(
            Guid branchId,
            DateOnly date,
            CancellationToken token = default);

        Task<Holiday?> GetHolidayByDateAsync(
            Guid branchId,
            DateOnly date,
            CancellationToken token = default);

        Task<IEnumerable<Holiday>> GetHolidaysInRangeAsync(
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
