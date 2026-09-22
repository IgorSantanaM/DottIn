using DottIn.Application.Exceptions;
using DottIn.Application.Features.TimeKeepings;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Employees;
using DottIn.Domain.HolidayCalendars;
using DottIn.Domain.TimeKeepings;
using MediatR;

namespace DottIn.Application.Features.TimeKeepings.Queries.GetHolidayWorkRecords;

public sealed class GetHolidayWorkRecordsQueryHandler(
    IBranchRepository branchRepository,
    IHolidayCalendarRepository holidayCalendarRepository,
    ITimeKeepingRepository timeKeepingRepository,
    ITimeKeepingAdjustmentRepository adjustmentRepository,
    IEmployeeRepository employeeRepository)
    : IRequestHandler<GetHolidayWorkRecordsQuery, IReadOnlyList<HolidayWorkRecordDto>>
{
    public async Task<IReadOnlyList<HolidayWorkRecordDto>> Handle(
        GetHolidayWorkRecordsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Year is < 1 or > 9999)
            throw new DomainException("Ano inválido.");

        var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken)
            ?? throw NotFoundException.ForEntity(nameof(Branch), request.BranchId);
        if (!branch.IsActive)
            throw new DomainException("A filial não está ativa.");

        var nowUtc = DateTime.UtcNow;
        var localToday = DateOnly.FromDateTime(BranchTime.ToLocal(nowUtc, branch.TimeZoneId));
        var start = new DateOnly(request.Year, 1, 1);
        if (start > localToday)
            return [];

        var yearEnd = new DateOnly(request.Year, 12, 31);
        var end = yearEnd < localToday ? yearEnd : localToday;
        var holidays = await holidayCalendarRepository.GetHolidaysInRangeAsync(
            request.BranchId, start, end, cancellationToken);
        var holidayNames = holidays
            .GroupBy(holiday => holiday.Date)
            .ToDictionary(group => group.Key, group => group.First().Name);
        if (holidayNames.Count == 0)
            return [];

        var records = await timeKeepingRepository.GetByBranchAndDatesAsync(
            request.BranchId, holidayNames.Keys.ToArray(), cancellationToken);
        if (records.Count == 0)
            return [];

        var adjustments = (await adjustmentRepository.GetApprovedByTimeKeepingIdsAsync(
                records.Select(record => record.Id), cancellationToken))
            .GroupBy(adjustment => adjustment.TimeKeepingId)
            .ToDictionary(group => group.Key, group => group.AsEnumerable());
        var employeeNames = await employeeRepository.GetNamesByIdsAsync(
            request.BranchId, records.Select(record => record.EmployeeId).Distinct().ToArray(), cancellationToken);

        return records.Select(record =>
        {
            adjustments.TryGetValue(record.Id, out var approved);
            var metrics = TimeKeepingMetricsCalculator.Calculate(record, nowUtc, approved);
            return new HolidayWorkRecordDto(
                employeeNames.TryGetValue(record.EmployeeId, out var name) ? name : "Desconhecido",
                record.WorkDate,
                holidayNames[record.WorkDate],
                metrics.TotalWorked);
        }).ToList();
    }
}