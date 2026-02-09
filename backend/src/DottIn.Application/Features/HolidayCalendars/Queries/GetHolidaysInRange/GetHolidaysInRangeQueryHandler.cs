using DottIn.Application.Exceptions;
using DottIn.Application.Features.HolidayCalendars.DTOs;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.HolidayCalendars;
using MediatR;
using System.Runtime.InteropServices;

namespace DottIn.Application.Features.HolidayCalendars.Queries.GetHolidaysInRange
{
    public class GetHolidaysInRangeQueryHandler(IBranchRepository branchRepository, 
        IHolidayCalendarRepository holidayCalendarRepository)
        : IRequestHandler<GetHolidaysInRangeQuery, IEnumerable<HolidayDto>>
    {
        public async Task<IEnumerable<HolidayDto>> Handle(GetHolidaysInRangeQuery request, CancellationToken cancellationToken)
        {
            var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);

            if (branch is null)
                throw NotFoundException.ForEntity(nameof(Branch), request.BranchId);

            if (!branch.IsActive)
                throw new DomainException("A empreas não esta ativa.");

            var holidays = await holidayCalendarRepository.GetHolidaysInRangeAsync(request.BranchId,
                request.StartDate, 
                request.EndDate,
                cancellationToken);

            return holidays.Select(h => new HolidayDto(h.Date, 
                            h.Name, 
                            h.Type, 
                            h.IsOptional));
        }
    }
}
