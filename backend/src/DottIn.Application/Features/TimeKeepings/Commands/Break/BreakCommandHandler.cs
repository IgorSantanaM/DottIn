using DottIn.Application.Exceptions;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Data;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Employees;
using DottIn.Domain.TimeKeepings;
using FluentValidation;
using MediatR;

namespace DottIn.Application.Features.TimeKeepings.Commands.Break
{
    public class BreakCommandHandler(IValidator<BreakCommand> validator,
                                        ITimeKeepingRepository timeKeepingRepository,
                                        IBranchRepository branchRepository,
                                        IEmployeeRepository employeeRepository,
                                        IUnitOfWork unitOfWork) : IRequestHandler<BreakCommand, Unit>
    {
        public async Task<Unit> Handle(BreakCommand request, CancellationToken cancellationToken)
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

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var existingTimeKeeping = await timeKeepingRepository.GetTodayByEmployeeForUpdateAsync(
                request.EmployeeId, today, cancellationToken);

            if (existingTimeKeeping is null)
                throw new NotFoundException("Nenhum registro de ponto encontrado para hoje. Faça o clock-in primeiro.");

            if (existingTimeKeeping.Status == TimeKeepingStatus.Finished)
                return Unit.Value;

            if (existingTimeKeeping.Status == TimeKeepingStatus.NotStarted)
                throw new DomainException("Clock-in não foi realizado. Faça o clock-in primeiro.");

            if (existingTimeKeeping.Status == TimeKeepingStatus.OnBreak)
            {
                existingTimeKeeping.EndBreak(DateTime.UtcNow);
            }
            else
            {
                existingTimeKeeping.StartBreak(DateTime.UtcNow);
            }

            await timeKeepingRepository.UpdateAsync(existingTimeKeeping);
            await unitOfWork.SaveChangesAsync();

            return Unit.Value;
        }
    }
}
