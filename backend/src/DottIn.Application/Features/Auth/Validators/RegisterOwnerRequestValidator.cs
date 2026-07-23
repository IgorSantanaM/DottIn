using DottIn.Application.Features.Auth.DTOs;
using FluentValidation;

namespace DottIn.Application.Features.Auth.Validators
{
    public class RegisterOwnerRequestValidator : AbstractValidator<RegisterOwnerRequest>
    {
        public RegisterOwnerRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("O nome deve ser informado.");

            RuleFor(x => x.Document)
                .NotEmpty()
                .WithMessage("O documento deve ser informado.")
                .ChildRules(document =>
                {
                    document.RuleFor(x => x.Value)
                        .NotEmpty()
                        .WithMessage("O valor do documento deve ser informado.")
                        .MinimumLength(11)
                        .WithMessage("O valor do documento deve conter pelo menos 11 caracteres.");

                    document.RuleFor(x => x.Type)
                        .NotEmpty()
                        .WithMessage("O tipo do documento deve ser informado.");
                });

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("A senha deve ser informada.")
                .MinimumLength(6)
                .WithMessage("A senha deve conter pelo menos 6 caracteres.");
        }
    }
}
