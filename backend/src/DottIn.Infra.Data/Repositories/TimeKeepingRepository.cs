using DottIn.Domain.TimeKeepings;
using DottIn.Infra.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace DottIn.Infra.Data.Repositories
{
    public class TimeKeepingRepository(DottInContext context) : Repository<TimeKeeping, Guid>(context), ITimeKeepingRepository
    {
        public Task<bool> ExistsForEmployeeOnDateAsync(Guid employeeId, DateOnly workDate, CancellationToken token = default)
            => context.TimeKeepings
                .AsNoTracking()
                .AnyAsync(tk => tk.EmployeeId == employeeId && tk.WorkDate == workDate, token);

        public async Task<IEnumerable<TimeKeeping>> GetActiveByBranchAsync(Guid branchId, CancellationToken token = default)
            => await context.TimeKeepings
                    .AsNoTracking()
                    .Include(tk => tk.Entries)
                    .Where(tk => tk.BranchId == branchId && tk.WorkDate == DateOnly.FromDateTime(DateTime.UtcNow))
                    .ToListAsync(token);

        public async Task<TimeKeeping?> GetActiveByEmployeeAsync(Guid employeeId, CancellationToken token = default)
            => await context.TimeKeepings
                    .AsNoTracking()
                    .Include(tk => tk.Entries)
                    .FirstOrDefaultAsync(tk => tk.EmployeeId == employeeId && tk.WorkDate == DateOnly.FromDateTime(DateTime.UtcNow), token);

        public async Task<IEnumerable<TimeKeeping>> GetByBranchAndDateAsync(Guid branchId, DateOnly workDate, CancellationToken token = default)
            => await context.TimeKeepings
                    .AsNoTracking()
                    .Include(tk => tk.Entries)
                    .Where(tk => tk.BranchId == branchId && tk.WorkDate == workDate)
                    .ToListAsync(token);

        public async Task<IEnumerable<TimeKeeping>> GetByEmployeeAndPeriodAsync(Guid employeeId, DateOnly startDate, DateOnly endDate, CancellationToken token = default)
            => await context.TimeKeepings
                    .AsNoTracking()
                    .Include(tk => tk.Entries)
                    .Where(tk => tk.EmployeeId == employeeId && tk.WorkDate >= startDate && tk.WorkDate <= endDate)
                    .OrderBy(tk => tk.WorkDate)
                    .ToListAsync(token);

        public async Task<TimeKeeping?> GetTodayByEmployeeAsync(Guid employeeId, DateOnly workDate, CancellationToken token = default)
            => await context.TimeKeepings
                    .AsNoTracking()
                    .Include(tk => tk.Entries)
                    .FirstOrDefaultAsync(tk => tk.EmployeeId == employeeId && tk.WorkDate == workDate, token);

        public async Task<TimeKeeping?> GetTodayByEmployeeForUpdateAsync(Guid employeeId, DateOnly workDate, CancellationToken token = default)
            => await context.TimeKeepings
                    .Include(tk => tk.Entries)
                    .FirstOrDefaultAsync(tk => tk.EmployeeId == employeeId && tk.WorkDate == workDate, token);
    }
}
