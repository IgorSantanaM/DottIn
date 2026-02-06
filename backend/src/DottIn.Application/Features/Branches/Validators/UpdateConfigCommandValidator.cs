using DottIn.Application.Features.Branches.Commands.UpdateConfig;
using FluentValidation;

namespace DottIn.Application.Features.Branches.Validators
{
    public class UpdateConfigCommandValidator : AbstractValidator<UpdateConfigCommand>
    {
        public UpdateConfigCommandValidator()
        {
            RuleFor(x => x.BranchId)
                .NotEmpty()
                .WithMessage("O ID da empresa deve ser informado.");

            RuleFor(x => x.AllowedRadiusMeters)
                .GreaterThan(0)
                .WithMessage("O raio permitido deve ser maior que zero.")
                .LessThanOrEqualTo(10000)
                .WithMessage("O raio permitido deve ser no máximo 10.000 metros.");

            RuleFor(x => x.TimeZoneId)
                .NotEmpty()
                .WithMessage("O fuso horário deve ser informado.")
                .MaximumLength(100)
                .WithMessage("O fuso horário deve ter no máximo 100 caracteres.");
        }
    }
}
