using DottIn.Application.Exceptions;
using DottIn.Application.Features.TimeKeepings.DTOs;
using DottIn.Application.Features.TimeKeepings;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Employees;
using DottIn.Domain.HolidayCalendars;
using DottIn.Domain.TimeKeepings;
using MediatR;

namespace DottIn.Application.Features.TimeKeepings.Queries.GetTimeKeepingByPeriod
{
    public class GetTimeKeepingByPeriodQueryHandler(ITimeKeepingRepository timeKeepingRepository,
        ITimeKeepingAdjustmentRepository adjustmentRepository,
        IEmployeeRepository employeeRepository,
        IBranchRepository branchRepository,
        IHolidayCalendarRepository holidayCalendarRepository)
        : IRequestHandler<GetTimeKeepingByPeriodQuery, IEnumerable<TimeKeepingRecordDto>>
    {
        public async Task<IEnumerable<TimeKeepingRecordDto>> Handle(GetTimeKeepingByPeriodQuery request, CancellationToken cancellationToken)
        {
            var endDate = TimeKeepingPeriod.NormalizeAndValidate(request.StartDate, request.EndDate);
            var employee = await employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);

            if (employee is null)
                throw NotFoundException.ForEntity(nameof(Employee), request.EmployeeId);

            if (!employee.IsActive)
                throw new DomainException("O Funcionário não esta ativo.");

            var branch = await branchRepository.GetByIdAsync(employee.BranchId, cancellationToken);

            if (branch is null)
                throw NotFoundException.ForEntity(nameof(Branch), employee.BranchId);

            if (!branch.IsActive)
                throw new DomainException("A Empresa não esta ativa.");

            var timeKeepings = (await timeKeepingRepository
                .GetByEmployeeAndPeriodAsync(request.EmployeeId, request.StartDate, endDate, cancellationToken)).ToList();
            var approvedAdjustments = (await adjustmentRepository.GetApprovedByTimeKeepingIdsAsync(
                    timeKeepings.Select(x => x.Id), cancellationToken))
                .GroupBy(x => x.TimeKeepingId)
                .ToDictionary(group => group.Key, group => group.AsEnumerable());

            var holidays = await holidayCalendarRepository.GetHolidaysInRangeAsync(
                branch.Id, request.StartDate, endDate);
            var holidayMap = holidays.ToDictionary(h => h.Date, h => h.Name);

            var records = timeKeepings.Select(tk =>
            {
                var now = DateTime.UtcNow;
                approvedAdjustments.TryGetValue(tk.Id, out var adjustments);
                var metrics = TimeKeepingMetricsCalculator.Calculate(tk, now, adjustments);
                var schedule = TimeKeepingMetricsCalculator.CalculateSchedule(
                    tk, employee, branch.ToleranceMinutes, metrics);

                return new TimeKeepingRecordDto(
                    tk.Id,
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
}
