using DottIn.Application.Exceptions;
using DottIn.Application.Features.HolidayCalendars.DTOs;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.HolidayCalendars;
using MediatR;

namespace DottIn.Application.Features.HolidayCalendars.Queries.GetHolidayCalendarByCountry
{
    public class GetHolidayCalendarByCountryQueryHandler(IBranchRepository branchRepository, IHolidayCalendarRepository holidayCalendarRepository) : IRequestHandler<GetHolidayCalendarByCountryQuery, HolidayCalendarSummaryDto>
    {
        public async Task<HolidayCalendarSummaryDto> Handle(GetHolidayCalendarByCountryQuery request, CancellationToken cancellationToken)
        {
            var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);

            if (branch is null)
                throw NotFoundException.ForEntity(nameof(Branch), request.BranchId);

            if (!branch.IsActive)
                throw new DomainException("A empreas não esta ativa.");

            var holidayCalendar = await holidayCalendarRepository.GetByCountryAndYearAsync(request.BranchId, request.CountryCode, request.Year, request.RegionCode, cancellationToken);

            if (holidayCalendar is null)
                throw NotFoundException.ForEntity(nameof(HolidayCalendar),
                    new { BranchId = request.BranchId, CountryCode =  request.CountryCode, Year = request.Year  } );

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
