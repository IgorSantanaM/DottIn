using DottIn.Application.Exceptions;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Data;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Employees;
using FluentValidation;
using MediatR;

namespace DottIn.Application.Features.Employees.Commands.UpdateSchedule
{
    public class UpdateScheduleCommandHandler(IBranchRepository branchRepository,
        IEmployeeRepository employeeRepository,
        IUnitOfWork unitOfWork,
        IValidator<UpdateScheduleCommand> validator)
        : IRequestHandler<UpdateScheduleCommand, bool>
    {
        public async Task<bool> Handle(UpdateScheduleCommand request, CancellationToken cancellationToken)
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);

            var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);

            if (branch is null)
                throw NotFoundException.ForEntity(nameof(Branch), request.BranchId);

            if (!branch.IsActive)
                throw new DomainException("A empresa não esta ativa.");

            var employee = await employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);

            if (employee is null)
                throw NotFoundException.ForEntity(nameof(Employee), request.EmployeeId);

            if (!employee.IsActive)
                throw new DomainException("O funcionário não esta ativo.");

            employee.UpdateSchedule(request.Start, request.End, request.IntervalStart, request.IntervalEnd);

            await employeeRepository.UpdateAsync(employee);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
