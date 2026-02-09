using DottIn.Application.Exceptions;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Data;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.HolidayCalendars;
using FluentValidation;
using MediatR;

namespace DottIn.Application.Features.HolidayCalendars.Commands.CreateHolidayCalendar
{
    public class CreateHolidayCalendarCommandHandler(IHolidayCalendarRepository holidayCalendarRepository,
        IBranchRepository branchRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateHolidayCalendarCommand> validator)
        : IRequestHandler<CreateHolidayCalendarCommand, Guid>
    {
        public async Task<Guid> Handle(CreateHolidayCalendarCommand request, CancellationToken cancellationToken)
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);

            var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);


            if (branch is null)
                throw NotFoundException.ForEntity(nameof(Branch), request.BranchId);

            if (!branch.IsActive)
                throw new DomainException("A Empresa não esta ativa.");

            var holidayCalendar = new HolidayCalendar(request.BranchId,
                            request.Name,
                            request.CountryCode,
                            request.Year,
                            request.RegionCode,
                            request.Description);

            await holidayCalendarRepository.AddAsync(holidayCalendar);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return holidayCalendar.Id;
        }
    }
}
