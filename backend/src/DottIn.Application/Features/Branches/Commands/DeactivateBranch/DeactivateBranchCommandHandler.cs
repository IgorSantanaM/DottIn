using DottIn.Application.Exceptions;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Data;
using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace DottIn.Application.Features.Branches.Commands.DeactivateBranch
{
    public class DeactivateBranchCommandHandler(IBranchRepository branchRepository, 
        IValidator<DeactivateBranchCommand> validator,
        IUnitOfWork unitOfWork) 
        : IRequestHandler<DeactivateBranchCommand, Unit>
    {
        public async Task<Unit> Handle(DeactivateBranchCommand request, CancellationToken cancellationToken)
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);  

            var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);

            if (branch is null)
                throw NotFoundException.ForEntity(nameof(Branch), request.BranchId);

            if (!branch.IsActive)
                return Unit.Value;

            branch.Deactivate();

            await branchRepository.UpdateAsync(branch);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
