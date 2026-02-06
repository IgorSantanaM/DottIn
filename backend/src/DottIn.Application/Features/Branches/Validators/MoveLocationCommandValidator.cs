using DottIn.Application.Features.Branches.Commands.MoveLocation;
using FluentValidation;

namespace DottIn.Application.Features.Branches.Validators
{
    public class MoveLocationCommandValidator : AbstractValidator<MoveLocationCommand>
    {
        public MoveLocationCommandValidator()
        {
            RuleFor(x => x.BranchId)
                .NotEmpty()
                .WithMessage("O ID da empresa deve ser informado.");

            RuleFor(x => x.NewAddress)
                .NotNull()
                .WithMessage("O novo endereço deve ser informado.");

            When(x => x.NewAddress is not null, () =>
            {
                RuleFor(x => x.NewAddress.Street)
                    .NotEmpty()
                    .WithMessage("A rua deve ser informada.")
                    .MaximumLength(200)
                    .WithMessage("A rua deve ter no máximo 200 caracteres.");

                RuleFor(x => x.NewAddress.Number)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("O número deve ser maior ou igual a zero.");

                RuleFor(x => x.NewAddress.City)
                    .NotEmpty()
                    .WithMessage("A cidade deve ser informada.")
                    .MaximumLength(100)
                    .WithMessage("A cidade deve ter no máximo 100 caracteres.");

                RuleFor(x => x.NewAddress.State)
                    .NotEmpty()
                    .WithMessage("O estado deve ser informado.")
                    .Length(2)
                    .WithMessage("O estado deve ter exatamente 2 caracteres (UF).");

                RuleFor(x => x.NewAddress.ZipCode)
                    .NotEmpty()
                    .WithMessage("O CEP deve ser informado.")
                    .Must(BeValidZipCode)
                    .WithMessage("CEP inválido. Deve conter 8 dígitos numéricos.");
            });

            RuleFor(x => x.NewGeolocation)
                .NotNull()
                .WithMessage("A nova geolocalização deve ser informada.");

            When(x => x.NewGeolocation is not null, () =>
            {
                RuleFor(x => x.NewGeolocation.Latitude)
                    .InclusiveBetween(-90.0, 90.0)
                    .WithMessage("Latitude deve estar entre -90 e 90.");

                RuleFor(x => x.NewGeolocation.Longitude)
                    .InclusiveBetween(-180.0, 180.0)
                    .WithMessage("Longitude deve estar entre -180 e 180.");
            });

            When(x => !string.IsNullOrEmpty(x.NewTimeZoneId), () =>
            {
                RuleFor(x => x.NewTimeZoneId)
                    .MaximumLength(100)
                    .WithMessage("O fuso horário deve ter no máximo 100 caracteres.");
            });
        }

        private static bool BeValidZipCode(string? zipCode)
        {
            if (string.IsNullOrWhiteSpace(zipCode))
                return false;

            var digitsOnly = new string(zipCode.Where(char.IsDigit).ToArray());
            return digitsOnly.Length == 8;
        }
    }
}
