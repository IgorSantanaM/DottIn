using DottIn.Application.Exceptions;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Data;
using FluentValidation;
using MediatR;

namespace DottIn.Application.Features.Branches.Commands.ActivateBranch
{
    public class ActivateBranchCommandHandler(IBranchRepository branchRepository,
        IValidator<ActivateBranchCommand> validator,
        IUnitOfWork unitOfWork)
        : IRequestHandler<ActivateBranchCommand, Unit>
    {
        public async Task<Unit> Handle(ActivateBranchCommand request, CancellationToken cancellationToken)
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);

            var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);

            if (branch is null)
                throw NotFoundException.ForEntity(nameof(Branch), request.BranchId);

            if (branch.IsActive)
                return Unit.Value;

            branch.Activate();

            await branchRepository.UpdateAsync(branch);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
