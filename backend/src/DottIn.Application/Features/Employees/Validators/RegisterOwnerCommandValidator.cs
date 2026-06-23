using DottIn.Application.Features.Employees.Commands.RegisterOwner;
using FluentValidation;

namespace DottIn.Application.Features.Employees.Validators;

public class RegisterOwnerCommandValidator : AbstractValidator<RegisterOwnerCommand>
{
    public RegisterOwnerCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("O nome do proprietário deve ser informado.")
            .MaximumLength(150)
            .WithMessage("O nome do proprietário deve ter no máximo 150 caracteres.");

        RuleFor(x => x.Document)
            .NotNull()
            .WithMessage("O documento deve ser informado.");

        When(x => x.Document is not null, () =>
        {
            RuleFor(x => x.Document.Value)
                .NotEmpty()
                .WithMessage("O número do CPF deve ser informado.")
                .Must(BeValidCpf)
                .WithMessage("CPF inválido. Deve conter 11 dígitos numéricos.");
        });

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("A senha deve ser informada.")
            .MinimumLength(6)
            .WithMessage("A senha deve ter no mínimo 6 caracteres.");
    }

    private static bool BeValidCpf(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return false;

        var digitsOnly = new string(cpf.Where(char.IsDigit).ToArray());
        return digitsOnly.Length == 11;
    }
}
