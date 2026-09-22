using DottIn.Application.Exceptions;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Data;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.HolidayCalendars;
using FluentValidation;
using MediatR;

namespace DottIn.Application.Features.Branches.Commands.SetComplianceRules
{
    public class SetComplianceRulesCommandHandler(IBranchRepository branchRepository,
        IHolidayCalendarRepository holidayCalendarRepository,
        IValidator<SetComplianceRulesCommand> validator,
        IUnitOfWork unitOfWork)
        : IRequestHandler<SetComplianceRulesCommand, Unit>
    {
        public async Task<Unit> Handle(SetComplianceRulesCommand request, CancellationToken cancellationToken)
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);

            var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);

            if (branch is null)
                throw NotFoundException.ForEntity(nameof(Branch), request.BranchId);

            if (!branch.IsActive)
                throw new DomainException("A Empresa não esta ativa.");

            if (request.HolidayCalendarId is { } calendarId)
            {
                var calendar = await holidayCalendarRepository.GetByIdAsync(calendarId, cancellationToken);
                if (calendar is null || calendar.BranchId != request.BranchId)
                    throw NotFoundException.ForEntity(nameof(HolidayCalendar), calendarId);
                if (!calendar.IsActive)
                    throw new DomainException("O calendário de feriados não está ativo.");
            }

            branch.SetComplianceRules(request.ToleranceMinutes, request.HolidayCalendarId);

            await branchRepository.UpdateAsync(branch);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;

        }
    }
}
