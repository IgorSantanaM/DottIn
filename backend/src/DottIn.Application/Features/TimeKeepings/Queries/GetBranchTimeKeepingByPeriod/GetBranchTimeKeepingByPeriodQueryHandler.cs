using DottIn.Application.Exceptions;
using DottIn.Application.Features.TimeKeepings.DTOs;
using DottIn.Application.Features.TimeKeepings;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Employees;
using DottIn.Domain.HolidayCalendars;
using DottIn.Domain.TimeKeepings;
using MediatR;

namespace DottIn.Application.Features.TimeKeepings.Queries.GetBranchTimeKeepingByPeriod;

public class GetBranchTimeKeepingByPeriodQueryHandler(
    ITimeKeepingRepository timeKeepingRepository,
    ITimeKeepingAdjustmentRepository adjustmentRepository,
    IEmployeeRepository employeeRepository,
    IBranchRepository branchRepository,
    IHolidayCalendarRepository holidayCalendarRepository)
    : IRequestHandler<GetBranchTimeKeepingByPeriodQuery, IEnumerable<BranchTimeKeepingRecordDto>>
{
    public async Task<IEnumerable<BranchTimeKeepingRecordDto>> Handle(
        GetBranchTimeKeepingByPeriodQuery request,
        CancellationToken cancellationToken)
    {
        var endDate = TimeKeepingPeriod.NormalizeAndValidate(request.StartDate, request.EndDate);
        var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);

        if (branch is null)
            throw NotFoundException.ForEntity(nameof(Branch), request.BranchId);

        if (!branch.IsActive)
            throw new DomainException("A Empresa não esta ativa.");

        var employees = await employeeRepository.GetByBranchIdAsync(request.BranchId, cancellationToken);
        var employeeMap = employees.ToDictionary(e => e.Id);

        var timeKeepings = (await timeKeepingRepository
            .GetByBranchAndPeriodAsync(request.BranchId, request.StartDate, endDate, cancellationToken)).ToList();
        var approvedAdjustments = (await adjustmentRepository.GetApprovedByTimeKeepingIdsAsync(
                timeKeepings.Select(x => x.Id), cancellationToken))
            .GroupBy(x => x.TimeKeepingId)
            .ToDictionary(group => group.Key, group => group.AsEnumerable());

        var now = DateTime.UtcNow;
        var holidays = await holidayCalendarRepository.GetHolidaysInRangeAsync(
            request.BranchId, request.StartDate, endDate);
        var holidayMap = holidays.ToDictionary(h => h.Date, h => h.Name);

        var records = timeKeepings.Select(tk =>
        {
            approvedAdjustments.TryGetValue(tk.Id, out var adjustments);
            var metrics = TimeKeepingMetricsCalculator.Calculate(tk, now, adjustments);
            employeeMap.TryGetValue(tk.EmployeeId, out var employee);
            var schedule = employee is null
                ? new ScheduleMetrics(TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero)
                : TimeKeepingMetricsCalculator.CalculateSchedule(tk, employee, branch.ToleranceMinutes, metrics);
            var employeeName = employee?.Name ?? "Desconhecido";

            return new BranchTimeKeepingRecordDto(
                tk.Id,
                tk.EmployeeId,
                employeeName,
                tk.WorkDate,
                metrics.ClockInUtc.HasValue ? BranchTime.ToLocal(metrics.ClockInUtc.Value, tk.TimeZoneId) : null,
                metrics.ClockOutUtc.HasValue ? BranchTime.ToLocal(metrics.ClockOutUtc.Value, tk.TimeZoneId) : null,
                metrics.TotalWorked,
                metrics.TotalBreak,
                metrics.Status.ToString(),
                metrics.NocturnalWorked > TimeSpan.Zero,
                tk.Source.ToString(),
                holidayMap.ContainsKey(tk.WorkDate),
                holidayMap.TryGetValue(tk.WorkDate, out var hName) ? hName : null,
                metrics.NocturnalWorked,
                schedule.ExpectedWorked,
                schedule.Late,
                schedule.EarlyDeparture,
                schedule.Overtime);
        });

        return records;
    }
}
