using DottIn.Application.Exceptions;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Data;
using DottIn.Domain.Core.Exceptions;
using FluentValidation;
using MediatR;

namespace DottIn.Application.Features.Branches.Commands.UpdateSchedule
{
    public class UpdateScheduleCommandHandler(IValidator<UpdateScheduleCommand> validator,
        IBranchRepository branchRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<UpdateScheduleCommand, Unit>
    {
        public async Task<Unit> Handle(UpdateScheduleCommand request, CancellationToken cancellationToken)
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);

            await validator.ValidateAndThrowAsync(request, cancellationToken);

            var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);

            if (branch is null)
                throw NotFoundException.ForEntity(nameof(Branch), request.BranchId);

            if (!branch.IsActive)
                throw new DomainException("A Empresa não esta ativa.");

            branch.UpdateSchedule(request.Start, request.End);

            await branchRepository.UpdateAsync(branch);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
