using DottIn.Application.Exceptions;
using DottIn.Application.Features.TimeKeepings.DTOs;
using DottIn.Application.Features.TimeKeepings;
using DottIn.Application.Shared.DTOS;
using DottIn.Domain.Branches;
using DottIn.Domain.Employees;
using DottIn.Domain.HolidayCalendars;
using DottIn.Domain.TimeKeepings;
using MediatR;

namespace DottIn.Application.Features.TimeKeepings.Queries.GetTimeKeepingById
{
    public class GetTimeKeepingByIdQueryHandler(
        ITimeKeepingRepository timeKeepingRepository,
        ITimeKeepingAdjustmentRepository adjustmentRepository,
        IBranchRepository branchRepository,
        IEmployeeRepository employeeRepository,
        IHolidayCalendarRepository holidayCalendarRepository)
        : IRequestHandler<GetTimeKeepingByIdQuery, TimeKeepingDetailsDto>
    {
        public async Task<TimeKeepingDetailsDto> Handle(GetTimeKeepingByIdQuery request, CancellationToken cancellationToken)
        {
            var timeKeeping = await timeKeepingRepository.GetWithEntriesByIdAsync(request.TimeKeepingId, cancellationToken);

            if (timeKeeping is null)
                throw NotFoundException.ForEntity(nameof(TimeKeeping), request.TimeKeepingId);

            var branch = await branchRepository.GetByIdAsync(timeKeeping.BranchId, cancellationToken);

            if (branch is null)
                throw NotFoundException.ForEntity(nameof(Branch), timeKeeping.BranchId);

            var employee = await employeeRepository.GetByIdAsync(timeKeeping.EmployeeId, cancellationToken);

            if (employee is null)
                throw NotFoundException.ForEntity(nameof(Employee), timeKeeping.EmployeeId);

            GeolocationDto geolocationDto = new(timeKeeping.Location!.Latitude, timeKeeping.Location.Longitude);

            var adjustments = await adjustmentRepository.GetApprovedByTimeKeepingIdsAsync(
                [timeKeeping.Id], cancellationToken);
            var effectiveEntries = TimeKeepingMetricsCalculator.BuildEffectiveEntries(timeKeeping, adjustments);
            var metrics = TimeKeepingMetricsCalculator.Calculate(timeKeeping, DateTime.UtcNow, adjustments);

            var isHoliday = await holidayCalendarRepository.IsHolidayAsync(branch.Id, timeKeeping.WorkDate);
            string? holidayName = null;
            if (isHoliday)
            {
                var holidays = await holidayCalendarRepository.GetHolidaysInRangeAsync(
                    branch.Id, timeKeeping.WorkDate, timeKeeping.WorkDate);
                holidayName = holidays.FirstOrDefault()?.Name;
            }

            TimeKeepingDetailsDto timeKeepingDetailsDto = new(employee.Name,
                                                        branch.Name,
                                                        metrics.Status,
                                                        timeKeeping.WorkDate,
                                                        BranchTime.ToLocal(timeKeeping.CreatedAt, timeKeeping.TimeZoneId),
                                                        geolocationDto,
                                                        effectiveEntries.Select(tke => new TimeEntryDto(
                                                            BranchTime.ToLocal(tke.Timestamp, timeKeeping.TimeZoneId),
                                                            tke.Type,
                                                            tke.Location is null ? null : new GeolocationDto(
                                                                tke.Location.Latitude, tke.Location.Longitude,
                                                                tke.AccuracyMeters, tke.CapturedAtUtc),
                                                            tke.Source)),
                                                        metrics.NocturnalWorked > TimeSpan.Zero,
                                                        timeKeeping.Source.ToString(),
                                                        isHoliday,
                                                        holidayName);

            return timeKeepingDetailsDto;
        }
    }
}
