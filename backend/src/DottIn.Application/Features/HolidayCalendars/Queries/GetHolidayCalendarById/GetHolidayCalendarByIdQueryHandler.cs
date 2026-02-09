using DottIn.Application.Exceptions;
using DottIn.Application.Features.HolidayCalendars.DTOs;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.HolidayCalendars;
using MediatR;

namespace DottIn.Application.Features.HolidayCalendars.Queries.GetHolidayCalendarById
{
    public class GetHolidayCalendarByIdQueryHandler(IBranchRepository branchRepository, IHolidayCalendarRepository holidayCalendarRepository) : IRequestHandler<GetHolidayCalendarByIdQuery, HolidayCalendarDetailsDto>
    {
        public async Task<HolidayCalendarDetailsDto> Handle(GetHolidayCalendarByIdQuery request, CancellationToken cancellationToken)
        {
            var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);

            if (branch is null)
                throw NotFoundException.ForEntity(nameof(Branch), request.BranchId);

            if (!branch.IsActive)
                throw new DomainException("A empreas não esta ativa.");

            var holidayCalendar = await holidayCalendarRepository.GetByIdAsync(request.HolidayCalendarId, cancellationToken);

            if(holidayCalendar is null)
                throw NotFoundException.ForEntity(nameof(HolidayCalendar), request.HolidayCalendarId);

            return new HolidayCalendarDetailsDto(branch.Name,
                                   holidayCalendar.Name,
                                   holidayCalendar.Description,
                                   holidayCalendar.CountryCode,
                                   holidayCalendar.RegionCode,
                                   holidayCalendar.Year,
                                   holidayCalendar.IsActive,
                                   holidayCalendar.CreatedAt,
                                   holidayCalendar.UpdatedAt);
        }
    }
}
