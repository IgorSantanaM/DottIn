using DottIn.Application.Exceptions;
using DottIn.Application.Features.HolidayCalendars.DTOs;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.HolidayCalendars;
using MediatR;

namespace DottIn.Application.Features.HolidayCalendars.Queries.GetHolidaysByDate
{
    public class GetHolidaysByDateQueryHandler(IBranchRepository branchRepository, IHolidayCalendarRepository holidayCalendarRepository) : IRequestHandler<GetHolidaysByDateQuery, HolidayDto>
    {
        public async Task<HolidayDto> Handle(GetHolidaysByDateQuery request, CancellationToken cancellationToken)
        {
            var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);

            if (branch is null)
                throw NotFoundException.ForEntity(nameof(Branch), request.BranchId);

            if (!branch.IsActive)
                throw new DomainException("A empreas não esta ativa.");

            var holiday = await holidayCalendarRepository.GetHolidayByDateAsync(request.BranchId, request.Date, cancellationToken);

            if (holiday is null)
                throw NotFoundException.ForEntity(nameof(HolidayCalendar), request.Date);

            return new HolidayDto(holiday.Date, holiday.Name, holiday.Type, holiday.IsOptional);
        }
    }
}
