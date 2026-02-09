using DottIn.Application.Exceptions;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Data;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.HolidayCalendars;
using FluentValidation;
using MediatR;

namespace DottIn.Application.Features.HolidayCalendars.Commands.RemoveHoliday
{
    public class RemoveHolidayCommandHandler(IBranchRepository branchRepository,
        IHolidayCalendarRepository holidayCalendarRepository,
        IUnitOfWork unitOfWork,
        IValidator<RemoveHolidayCommand> validator) : IRequestHandler<RemoveHolidayCommand, Unit>
    {
        public async Task<Unit> Handle(RemoveHolidayCommand request, CancellationToken cancellationToken)
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);

            var holidayCalendar = await holidayCalendarRepository.GetByIdAsync(request.HolidayCalendarId, cancellationToken);

            if (holidayCalendar is null)
                throw NotFoundException.ForEntity(nameof(HolidayCalendar), request.HolidayCalendarId);

            if (!holidayCalendar.IsActive)
                throw new DomainException("O calendário não esta ativo.");

            var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);

            if (branch is null)
                throw NotFoundException.ForEntity(nameof(Branch), request.BranchId);

            if (!branch.IsActive)
                throw new DomainException("A empreas não esta ativa.");

            holidayCalendar.RemoveHoliday(request.Date);

            await holidayCalendarRepository.UpdateAsync(holidayCalendar);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
