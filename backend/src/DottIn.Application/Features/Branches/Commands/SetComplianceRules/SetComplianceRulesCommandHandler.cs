using DottIn.Application.Exceptions;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Data;
using DottIn.Domain.Core.Exceptions;
using FluentValidation;
using MediatR;

namespace DottIn.Application.Features.Branches.Commands.SetComplianceRules
{
    public class SetComplianceRulesCommandHandler(IBranchRepository branchRepository,
        IValidator<SetComplianceRulesCommand> validator,
        IUnitOfWork unitOfWork)
        : IRequestHandler<SetComplianceRulesCommand, Unit>
    {
        public async Task<Unit> Handle(SetComplianceRulesCommand request, CancellationToken cancellationToken)
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);

            var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);

            if (branch is null)
                throw NotFoundException.ForEntity(nameof(Branch), request.BranchId);

            if (!branch.IsActive)
                throw new DomainException("A Empresa não esta ativa.");

            // TODO: Check calendar.

            branch.SetComplianceRules(request.ToleranceMinutes, request.HolidayCalendarId);

            await branchRepository.UpdateAsync(branch);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;

        }
    }
}
