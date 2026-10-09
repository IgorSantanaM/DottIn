using DottIn.Application.Exceptions;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Data;
using DottIn.Application.Features.Subscriptions.Services;
using DottIn.Domain.Core.Exceptions;
using FluentValidation;
using MediatR;

namespace DottIn.Application.Features.Branches.Commands.ActivateBranch
{
    public class ActivateBranchCommandHandler(IBranchRepository branchRepository,
        IValidator<ActivateBranchCommand> validator,
        IUnitOfWork unitOfWork,
        ITenantSubscriptionService subscriptions)
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

            await unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                if (!branch.OwnerId.HasValue)
                    throw new DomainException("Filial sem proprietário vinculado.");
                await branchRepository.LockOwnerAsync(branch.OwnerId.Value, token);
                // Refresh after acquiring the lock so two activation requests do not consume two slots.
                await branchRepository.ReloadAsync(branch, token);
                if (branch.IsActive) return;
                if (!await subscriptions.CanAddBranchAsync(branch.OwnerId!.Value, token))
                    throw new SubscriptionLimitExceededException("O plano não permite ativar mais unidades. Confira a assinatura e o limite de filiais.");
                branch.Activate();
                await branchRepository.UpdateAsync(branch);
                await unitOfWork.SaveChangesAsync(token);
            }, cancellationToken);

            return Unit.Value;
        }
    }
}
