using DottIn.Application.Features.Branches.Commands.SetOwner;
using FluentValidation;

namespace DottIn.Application.Features.Branches.Validators
{
    public class SetOwnerCommandValidator : AbstractValidator<SetOwnerCommand>
    {
        public SetOwnerCommandValidator()
        {
            RuleFor(x => x.BranchId)
                .NotEmpty()
                .WithMessage("O ID da empresa deve ser informado.");

            RuleFor(x => x.EmployeeId)
                .NotEmpty()
                .WithMessage("O ID do funcionário deve ser informado.");
        }
    }
}
