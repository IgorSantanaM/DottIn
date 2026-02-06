using DottIn.Application.Exceptions;
using DottIn.Application.Features.Branches.Commands.ActivateBranch;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Data;
using DottIn.Domain.Core.Exceptions;
using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace DottIn.Application.Features.Branches.Commands.UpdateConfig
{
    public class UpdateConfigCommandHandler(IValidator<UpdateConfigCommand> validator, 
        IBranchRepository branchRepository,
        IUnitOfWork unitOfWork) 
        : IRequestHandler<UpdateConfigCommand, Unit>
    {
        public async Task<Unit> Handle(UpdateConfigCommand request, CancellationToken cancellationToken)
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);

            var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);

            if (branch is null)
                throw NotFoundException.ForEntity(nameof(Branch), request.BranchId);

            if (!branch.IsActive)
                throw new DomainException("A Empresa não esta ativa.");

            branch.UpdateConfig(request.AllowedRadiusMeters, request.TimeZoneId);

            await branchRepository.UpdateAsync(branch);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
