using DottIn.Application.Exceptions;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Data;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Employees;
using DottIn.Domain.TimeKeepings;
using FluentValidation;
using MediatR;

namespace DottIn.Application.Features.TimeKeepings.Commands.ClockIn
{
    public class ClockInCommandHandler(IValidator<ClockInCommand> validator,
                                        ITimeKeepingRepository timeKeepingRepository,
                                        IBranchRepository branchRepository,
                                        IEmployeeRepository employeeRepository,
                                        IUnitOfWork unitOfWork)
                                        : IRequestHandler<ClockInCommand, Guid>
    {
        public async Task<Guid> Handle(ClockInCommand request, CancellationToken cancellationToken)
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);

            var employee = await employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);

            if (employee is null)
                throw NotFoundException.ForEntity(nameof(Employee), request.EmployeeId);

            if (!employee.IsActive)
                throw new DomainException("Funcionário está desativado e não poderá bater ponto.");

            var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);

            if (branch is null)
                throw NotFoundException.ForEntity(nameof(Branch), request.BranchId);

            if (!branch.IsActive)
                throw new DomainException("A empresa esta desativada e não poderá utilizar o sistem de ponto.");

            if (!branch.IsWithinRange(request.GeolocationDto.Latitude, request.GeolocationDto.Longitude))
                throw new DomainException("Funcionário esta fora do raio permitido para bater o ponto.");

            // TODO: Verify if its weekend or holiday.

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var existingTimeKeeping = await timeKeepingRepository.GetTodayByEmployeeAsync(
                request.EmployeeId, today, cancellationToken);

            if (existingTimeKeeping is not null)
                return existingTimeKeeping.Id;

            var geolocation = new Geolocation(request.GeolocationDto.Latitude, request.GeolocationDto.Longitude);

            var timeKeeping = new TimeKeeping(request.BranchId, request.EmployeeId, geolocation);

            timeKeeping.ClockIn(DateTime.UtcNow);

            await timeKeepingRepository.AddAsync(timeKeeping, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return timeKeeping.Id;
        }
    }
}
