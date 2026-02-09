using DottIn.Application.Exceptions;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Data;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Employees;
using FluentValidation;
using MediatR;

namespace DottIn.Application.Features.Employees.Commands.DeactivateEmployee
{
    public class DeactivateEmployeeCommandHandler(IEmployeeRepository employeeRepository,
        IBranchRepository branchRepository,
        IUnitOfWork unitOfWork,
        IValidator<DeactivateEmployeeCommand> validator) : IRequestHandler<DeactivateEmployeeCommand, Unit>
    {
        public async Task<Unit> Handle(DeactivateEmployeeCommand request, CancellationToken cancellationToken)
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);
            var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);

            if (branch is null)
                throw NotFoundException.ForEntity(nameof(Branch), request.BranchId);

            if (!branch.IsActive)
                throw new DomainException("A empresa não está ativa.");

            var employee = await employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);

            if (employee is null)
                throw NotFoundException.ForEntity(nameof(Employee), request.EmployeeId);

            if (!employee.IsActive)
                return Unit.Value;

            employee.Deactivate();

            await employeeRepository.UpdateAsync(employee);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
