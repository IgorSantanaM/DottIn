using DottIn.Application.Features.Branches.Commands.DeactivateBranch;
using FluentValidation;

namespace DottIn.Application.Features.Branches.Validators
{
    public class DeactivateBranchCommandValidator : AbstractValidator<DeactivateBranchCommand>
    {
        public DeactivateBranchCommandValidator()
        {
            RuleFor(x => x.BranchId)
                .NotEmpty()
                .WithMessage("O ID da empresa deve ser informado.");
        }
    }
}
