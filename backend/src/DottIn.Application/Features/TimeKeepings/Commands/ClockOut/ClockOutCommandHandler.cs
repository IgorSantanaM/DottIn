using DottIn.Application.Exceptions;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Data;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Employees;
using DottIn.Domain.TimeKeepings;
using FluentValidation;
using MediatR;

namespace DottIn.Application.Features.TimeKeepings.Commands.ClockOut
{
    public class ClockOutCommandHandler(
        IValidator<ClockOutCommand> validator,
        IBranchRepository branchRepository,
        ITimeKeepingRepository timeKeepingRepository,
        IEmployeeRepository employeeRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<ClockOutCommand, Unit>
    {
        public async Task<Unit> Handle(ClockOutCommand request, CancellationToken cancellationToken)
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);

            var employee = await employeeRepository.GetByIdAsync(request.EmployeeId);

            if (employee is null)
                throw NotFoundException.ForEntity(nameof(Employee), request.EmployeeId);

            if (!employee.IsActive)
                throw new DomainException("Funcionário está desativado e não pode bater ponto.");

            var branch = await branchRepository.GetByIdAsync(request.BranchId);

            if (branch is null)
                throw NotFoundException.ForEntity(nameof(Branch), request.BranchId);

            if (!branch.IsActive)
                throw new DomainException("A empresa está desativada e não pode utilizar o sistema de ponto.");

            if (!branch.IsWithinRange(request.GeolocationDto.Latitude, request.GeolocationDto.Longitude))
                throw new DomainException("Funcionário está fora do raio permitido para bater o ponto.");

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var existingTimeKeeping = await timeKeepingRepository.GetTodayByEmployeeForUpdateAsync(
                request.EmployeeId, today, cancellationToken);

            if (existingTimeKeeping is null)
                throw new NotFoundException("Nenhum registro de ponto encontrado para hoje. Faça o clock-in primeiro.");

            if (existingTimeKeeping.Status == TimeKeepingStatus.Finished)
                return Unit.Value;

            if (existingTimeKeeping.Status == TimeKeepingStatus.NotStarted)
                throw new DomainException("Clock-in não foi realizado. Faça o clock-in primeiro.");

            existingTimeKeeping.ClockOut(DateTime.UtcNow);

            await timeKeepingRepository.UpdateAsync(existingTimeKeeping);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
