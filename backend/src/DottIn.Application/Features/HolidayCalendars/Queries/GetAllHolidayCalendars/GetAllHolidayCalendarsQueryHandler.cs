using DottIn.Application.Exceptions;
using DottIn.Application.Features.HolidayCalendars.DTOs;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.HolidayCalendars;
using MediatR;

namespace DottIn.Application.Features.HolidayCalendars.Queries.GetAllHolidayCalendars
{
    public class GetAllHolidayCalendarsQueryHandler(IBranchRepository branchRepository,
        IHolidayCalendarRepository holidayCalendarRepository) : IRequestHandler<GetAllHolidayCalendarsQuery, IEnumerable<HolidayCalendarSummaryDto>>
    {
        public async Task<IEnumerable<HolidayCalendarSummaryDto>> Handle(GetAllHolidayCalendarsQuery request, CancellationToken cancellationToken)
        {

            var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);

            if (branch is null)
                throw NotFoundException.ForEntity(nameof(Branch), request.BranchId);

            if (!branch.IsActive)
                throw new DomainException("A empreas não esta ativa.");

            var holidayCalendars = await holidayCalendarRepository.GetAllAsync(request.BranchId, cancellationToken);

            return holidayCalendars.Select(h => new HolidayCalendarSummaryDto(branch.Name,
                               h.Name,
                               h.Description,
                               h.CountryCode,
                               h.RegionCode,
                               h.Year,
                               h.IsActive));
        }
    }
}
