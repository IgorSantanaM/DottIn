using DottIn.Application.Exceptions;
using DottIn.Application.Features.HolidayCalendars.DTOs;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.HolidayCalendars;
using MediatR;

namespace DottIn.Application.Features.HolidayCalendars.Queries.GetHolidayCalendarByYear
{
    public class GetHolidayCalendarByYearQueryHandler(IBranchRepository branchRepository, IHolidayCalendarRepository holidayCalendarRepository) : IRequestHandler<GetHolidayCalendarByYearQuery, HolidayCalendarSummaryDto>
    {
        public async Task<HolidayCalendarSummaryDto> Handle(GetHolidayCalendarByYearQuery request, CancellationToken cancellationToken)
        {
            var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);

            if (branch is null)
                throw NotFoundException.ForEntity(nameof(Branch), request.BranchId);

            if (!branch.IsActive)
                throw new DomainException("A empreas não esta ativa.");

            var holidayCalendar = await holidayCalendarRepository.GetByYearAsync(request.BranchId, request.Year, cancellationToken);

            if (holidayCalendar is null)
                throw NotFoundException.ForEntity(nameof(HolidayCalendar), new { BranchId = request.BranchId, Year = request.Year });

            return new HolidayCalendarSummaryDto(branch.Name,
                                 holidayCalendar.Name,
                                 holidayCalendar.Description,
                                 holidayCalendar.CountryCode,
                                 holidayCalendar.RegionCode,
                                 holidayCalendar.Year,
                                 holidayCalendar.IsActive);
        }
    }
}
