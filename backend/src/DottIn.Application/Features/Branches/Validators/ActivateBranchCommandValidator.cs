using DottIn.Application.Features.Branches.Commands.ActivateBranch;
using FluentValidation;

namespace DottIn.Application.Features.Branches.Validators
{
    public class ActivateBranchCommandValidator : AbstractValidator<ActivateBranchCommand>
    {
        public ActivateBranchCommandValidator()
        {
            RuleFor(x => x.BranchId)
                .NotEmpty()
                .WithMessage("O ID da empresa deve ser informado.");
        }
    }
}
