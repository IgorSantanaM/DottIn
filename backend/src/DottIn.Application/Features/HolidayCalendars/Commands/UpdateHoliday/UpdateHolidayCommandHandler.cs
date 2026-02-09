using DottIn.Application.Exceptions;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Data;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.HolidayCalendars;
using FluentValidation;
using MediatR;

namespace DottIn.Application.Features.HolidayCalendars.Commands.UpdateHoliday
{
    public class UpdateHolidayCommandHandler(IBranchRepository branchRepository,
        IHolidayCalendarRepository holidayCalendarRepository,
        IValidator<UpdateHolidayCommand> validator,
        IUnitOfWork unitOfWork)
        : IRequestHandler<UpdateHolidayCommand, Unit>
    {
        public async Task<Unit> Handle(UpdateHolidayCommand request, CancellationToken cancellationToken)
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);

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

            holidayCalendar.UpdateHoliday(request.NewDate, request.NewName, request.NewType, request.IsOptional);

            await holidayCalendarRepository.UpdateAsync(holidayCalendar);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
