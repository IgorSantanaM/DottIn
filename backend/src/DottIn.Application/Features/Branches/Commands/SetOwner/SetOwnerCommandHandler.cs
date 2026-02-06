using DottIn.Application.Exceptions;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Data;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Employees;
using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace DottIn.Application.Features.Branches.Commands.SetOwner
{
    public class SetOwnerCommandHandler(IValidator<SetOwnerCommand> validator,
        IBranchRepository branchRepository, 
        IEmployeeRepository employeeRepository, 
        IUnitOfWork unitOfWork) 
        : IRequestHandler<SetOwnerCommand, Unit>
    {
        public async Task<Unit> Handle(SetOwnerCommand request, CancellationToken cancellationToken)
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);

            var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);

            if(branch is null)
                throw NotFoundException.ForEntity(nameof(Branch), request.BranchId);

            if(branch.OwnerId == request.EmployeeId)
                return Unit.Value;

            if (!branch.IsActive)
                throw new DomainException("A Empresa não esta ativa.");

            var employee = await employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);

            if(employee is null)
                throw NotFoundException.ForEntity(nameof(Employee), request.EmployeeId);

            if(!employee.IsActive)
                throw new DomainException("O Funcionário não esta ativo.");

            branch.SetOwner(request.EmployeeId);

            await branchRepository.UpdateAsync(branch); 

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;  
        }
    }
}
