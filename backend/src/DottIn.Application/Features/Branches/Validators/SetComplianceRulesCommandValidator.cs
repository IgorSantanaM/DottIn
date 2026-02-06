using DottIn.Application.Features.Branches.Commands.SetComplianceRule;
using FluentValidation;

namespace DottIn.Application.Features.Branches.Validators
{
    public class SetComplianceRulesCommandValidator : AbstractValidator<SetComplianceRulesCommand>
    {
        public SetComplianceRulesCommandValidator()
        {
            RuleFor(x => x.BranchId)
                .NotEmpty()
                .WithMessage("O ID da empresa deve ser informado.");

            RuleFor(x => x.ToleranceMinutes)
                .GreaterThanOrEqualTo(0)
                .WithMessage("A tolerância deve ser maior ou igual a zero.")
                .LessThanOrEqualTo(60)
                .WithMessage("A tolerância deve ser no máximo 60 minutos.");
        }
    }
}
