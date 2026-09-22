using DottIn.Application.Exceptions;
using DottIn.Application.Features.TimeKeepings.DTOs;
using DottIn.Domain.Branches;
using DottIn.Domain.Common;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Employees;
using DottIn.Domain.HolidayCalendars;
using DottIn.Domain.TimeKeepings;
using MediatR;

namespace DottIn.Application.Features.TimeKeepings.Queries.GetPagedEmployeeTimeKeeping;

public record GetPagedEmployeeTimeKeepingQuery(
    Guid EmployeeId,
    DateOnly StartDate,
    DateOnly? EndDate,
    int PageNumber,
    int PageSize)
    : IRequest<PagedResult<TimeKeepingRecordDto>>;

public sealed class GetPagedEmployeeTimeKeepingQueryHandler(
    ITimeKeepingRepository timeKeepingRepository,
    ITimeKeepingAdjustmentRepository adjustmentRepository,
    IEmployeeRepository employeeRepository,
    IBranchRepository branchRepository,
    IHolidayCalendarRepository holidayCalendarRepository)
    : IRequestHandler<GetPagedEmployeeTimeKeepingQuery, PagedResult<TimeKeepingRecordDto>>
{
    public async Task<PagedResult<TimeKeepingRecordDto>> Handle(
        GetPagedEmployeeTimeKeepingQuery request,
        CancellationToken cancellationToken)
    {
        if (request.PageNumber < 1)
            throw new DomainException("O número da página deve ser maior que zero.");
        if (request.PageSize is < 10 or > 100)
            throw new DomainException("O tamanho da página deve estar entre 10 e 100 registros.");

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

        var (timeKeepings, totalCount) = await timeKeepingRepository.GetPagedByEmployeeAndPeriodAsync(
            request.EmployeeId, request.StartDate, endDate,
            request.PageNumber, request.PageSize, cancellationToken);
        if (timeKeepings.Count == 0)
            return new PagedResult<TimeKeepingRecordDto>
            {
                Items = [],
                TotalCount = totalCount,
                TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)request.PageSize)
            };

        var approvedAdjustments = (await adjustmentRepository.GetApprovedByTimeKeepingIdsAsync(
                timeKeepings.Select(x => x.Id), cancellationToken))
            .GroupBy(x => x.TimeKeepingId)
            .ToDictionary(group => group.Key, group => group.AsEnumerable());
        var holidays = await holidayCalendarRepository.GetHolidaysInRangeAsync(
            branch.Id, request.StartDate, endDate);
        var holidayMap = holidays.ToDictionary(h => h.Date, h => h.Name);
        var now = DateTime.UtcNow;

        var records = timeKeepings.Select(tk =>
        {
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
                holidayMap.TryGetValue(tk.WorkDate, out var holidayName) ? holidayName : null,
                metrics.NocturnalWorked,
                schedule.ExpectedWorked,
                schedule.Late,
                schedule.EarlyDeparture,
                schedule.Overtime);
        }).ToList();

        return new PagedResult<TimeKeepingRecordDto>
        {
            Items = records,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };
    }
}
