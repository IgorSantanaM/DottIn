using DottIn.Application.Exceptions;
using DottIn.Application.Features.TimeKeepings.DTOs;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Employees;
using DottIn.Domain.TimeKeepings;
using MediatR;

namespace DottIn.Application.Features.TimeKeepings.Queries.GetBranchTimeKeepingByPeriod;

public class GetBranchTimeKeepingByPeriodQueryHandler(
    ITimeKeepingRepository timeKeepingRepository,
    IEmployeeRepository employeeRepository,
    IBranchRepository branchRepository)
    : IRequestHandler<GetBranchTimeKeepingByPeriodQuery, IEnumerable<BranchTimeKeepingRecordDto>>
{
    public async Task<IEnumerable<BranchTimeKeepingRecordDto>> Handle(
        GetBranchTimeKeepingByPeriodQuery request,
        CancellationToken cancellationToken)
    {
        var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);

        if (branch is null)
            throw NotFoundException.ForEntity(nameof(Branch), request.BranchId);

        if (!branch.IsActive)
            throw new DomainException("A Empresa não esta ativa.");

        var employees = await employeeRepository.GetByBranchIdAsync(request.BranchId, cancellationToken);
        var employeeMap = employees.ToDictionary(e => e.Id, e => e.Name);

        var timeKeepings = await timeKeepingRepository
            .GetByBranchAndPeriodAsync(request.BranchId, request.StartDate, request.EndDate, cancellationToken);

        var now = DateTime.UtcNow;
        var tz = TimeZoneInfo.FindSystemTimeZoneById(branch.TimeZoneId);

        var records = timeKeepings.Select(tk =>
        {
            var clockIn = tk.Entries.FirstOrDefault(e => e.Type == TimeKeepingType.ClockIn)?.Timestamp;
            var clockOut = tk.Entries.FirstOrDefault(e => e.Type == TimeKeepingType.ClockOut)?.Timestamp;
            var effectiveEnd = clockOut ?? (clockIn.HasValue ? now : (DateTime?)null);

            var totalWorked = clockIn.HasValue && effectiveEnd.HasValue && effectiveEnd > clockIn
                ? effectiveEnd.Value - clockIn.Value
                : TimeSpan.Zero;

            var breaks = tk.Entries
                .Where(e => e.Type == TimeKeepingType.BreakStart || e.Type == TimeKeepingType.BreakEnd)
                .OrderBy(e => e.Timestamp)
                .ToList();

            var totalBreak = TimeSpan.Zero;
            for (int i = 0; i < breaks.Count; i++)
            {
                if (breaks[i].Type == TimeKeepingType.BreakStart)
                {
                    var breakEnd = (i + 1 < breaks.Count && breaks[i + 1].Type == TimeKeepingType.BreakEnd)
                        ? breaks[i + 1].Timestamp
                        : now;
                    totalBreak += breakEnd - breaks[i].Timestamp;
                    if (i + 1 < breaks.Count && breaks[i + 1].Type == TimeKeepingType.BreakEnd)
                        i++;
                }
            }

            if (totalWorked > TimeSpan.Zero)
                totalWorked -= totalBreak;

            var employeeName = employeeMap.TryGetValue(tk.EmployeeId, out var name) ? name : "Desconhecido";

            var isNocturnal = false;
            if (clockIn.HasValue)
            {
                var localHour = TimeZoneInfo.ConvertTimeFromUtc(clockIn.Value, tz).Hour;
                isNocturnal = localHour >= 22 || localHour < 6;
            }

            return new BranchTimeKeepingRecordDto(
                tk.Id,
                tk.EmployeeId,
                employeeName,
                tk.WorkDate,
                clockIn,
                clockOut,
                totalWorked,
                totalBreak,
                tk.Status.ToString(),
                isNocturnal);
        });

        return records;
    }
}
