using DottIn.Domain.HolidayCalendars;
using DottIn.Infra.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace DottIn.Infra.Data.Repositories
{
    public class HolidayCalendarRepository(DottInContext context)
        : Repository<HolidayCalendar, Guid>(context), IHolidayCalendarRepository
    {
        public async Task<HolidayCalendar?> GetByYearAsync(
            Guid branchId,
            int year,
            CancellationToken token = default)
            => await context.HolidayCalendars
                .AsNoTracking()
                .Include(hc => hc.Holidays)
                .FirstOrDefaultAsync(hc =>
                    hc.BranchId == branchId &&
                    hc.Year == year &&
                    hc.IsActive, token);

        public async Task<IEnumerable<HolidayCalendar>> GetAllAsync(
            Guid branchId,
            CancellationToken token = default)
            => await context.HolidayCalendars
                .AsNoTracking()
                .Include(hc => hc.Holidays)
                .Where(hc => hc.BranchId == branchId)
                .OrderByDescending(hc => hc.Year)
                .ToListAsync(token);

        public async Task<IEnumerable<HolidayCalendar>> GetActiveAsync(
            Guid branchId,
            CancellationToken token = default)
            => await context.HolidayCalendars
                .AsNoTracking()
                .Include(hc => hc.Holidays)
                .Where(hc => hc.BranchId == branchId && hc.IsActive)
                .OrderByDescending(hc => hc.Year)
                .ToListAsync(token);

        public async Task<HolidayCalendar?> GetByCountryAndYearAsync(
            Guid branchId,
            string countryCode,
            int year,
            string? regionCode = null,
            CancellationToken token = default)
            => await context.HolidayCalendars
                .AsNoTracking()
                .Include(hc => hc.Holidays)
                .FirstOrDefaultAsync(hc =>
                    hc.BranchId == branchId &&
                    hc.CountryCode == countryCode.ToUpperInvariant() &&
                    hc.Year == year &&
                    hc.RegionCode == (regionCode != null ? regionCode.ToUpperInvariant() : null) &&
                    hc.IsActive, token);

        public async Task<bool> IsHolidayAsync(
            Guid branchId,
            DateOnly date,
            CancellationToken token = default)
        {
            var year = date.Year;

            return await context.HolidayCalendars
                .AsNoTracking()
                .Where(hc => hc.BranchId == branchId && hc.Year == year && hc.IsActive)
                .SelectMany(hc => hc.Holidays)
                .AnyAsync(h => h.Date == date, token);
        }

        public async Task<bool> IsMandatoryHolidayAsync(
            Guid branchId,
            DateOnly date,
            CancellationToken token = default)
        {
            var year = date.Year;

            return await context.HolidayCalendars
                .AsNoTracking()
                .Where(hc => hc.BranchId == branchId && hc.Year == year && hc.IsActive)
                .SelectMany(hc => hc.Holidays)
                .AnyAsync(h => h.Date == date && !h.IsOptional, token);
        }

        public async Task<Holiday?> GetHolidayByDateAsync(
            Guid branchId,
            DateOnly date,
            CancellationToken token = default)
        {
            var year = date.Year;

            var calendar = await context.HolidayCalendars
                .AsNoTracking()
                .Include(hc => hc.Holidays)
                .FirstOrDefaultAsync(hc =>
                    hc.BranchId == branchId &&
                    hc.Year == year &&
                    hc.IsActive, token);

            return calendar?.Holidays.FirstOrDefault(h => h.Date == date);
        }

        public async Task<IEnumerable<Holiday>> GetHolidaysInRangeAsync(
            Guid branchId,
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken token = default)
        {
            var startYear = startDate.Year;
            var endYear = endDate.Year;

            var calendars = await context.HolidayCalendars
                .AsNoTracking()
                .Include(hc => hc.Holidays)
                .Where(hc =>
                    hc.BranchId == branchId &&
                    hc.Year >= startYear &&
                    hc.Year <= endYear &&
                    hc.IsActive)
                .ToListAsync(token);

            return calendars
                .SelectMany(hc => hc.Holidays)
                .Where(h => h.Date >= startDate && h.Date <= endDate)
                .OrderBy(h => h.Date);
        }

        public async Task<HolidayCalendar?> GetWithHolidaysAsync(
            Guid id,
            CancellationToken token = default)
            => await context.HolidayCalendars
                .AsNoTracking()
                .Include(hc => hc.Holidays)
                .FirstOrDefaultAsync(hc => hc.Id == id, token);

        public async Task<HolidayCalendar?> GetForUpdateAsync(
            Guid id,
            CancellationToken token = default)
            => await context.HolidayCalendars
                .Include(hc => hc.Holidays)
                .FirstOrDefaultAsync(hc => hc.Id == id, token);

        public Task<bool> ExistsForBranchAndYearAsync(
            Guid branchId,
            int year,
            CancellationToken token = default)
            => context.HolidayCalendars
                .AsNoTracking()
                .AnyAsync(hc => hc.BranchId == branchId && hc.Year == year, token);
    }
}
