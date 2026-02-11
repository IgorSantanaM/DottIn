using DottIn.Application.Features.Branches.Commands.CreateBranch;
using DottIn.Domain.ValueObjects;
using FluentValidation;

namespace DottIn.Application.Features.Branches.Validators
{
    public class CreateBranchCommandValidator : AbstractValidator<CreateBranchCommand>
    {
        private static readonly HashSet<string> ValidTimeZones = new(TimeZoneInfo.GetSystemTimeZones().Select(tz => tz.Id));

        public CreateBranchCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("O nome da empresa deve ser informado.")
                .MaximumLength(150)
                .WithMessage("O nome da empresa deve ter no máximo 150 caracteres.")
                .Must(name => !string.IsNullOrWhiteSpace(name) && name.Trim().Length >= 2)
                .WithMessage("O nome da empresa deve ter pelo menos 2 caracteres.");

            RuleFor(x => x.Document)
                .NotNull()
                .WithMessage("O documento deve ser informado.");

            When(x => x.Document is not null, () =>
            {
                RuleFor(x => x.Document.Value)
                    .NotEmpty()
                    .WithMessage("O número do documento deve ser informado.")
                    .Must(BeValidCnpjFormat)
                    .WithMessage("CNPJ inválido. Deve conter 14 dígitos numéricos.")
                    .Must(BeValidCnpjChecksum)
                    .WithMessage("CNPJ inválido. Dígitos verificadores incorretos.");

                RuleFor(x => x.Document.Type)
                    .Must(type => type == DocumentType.CNPJ)
                    .WithMessage("Empresa deve ser registrada com um CNPJ.");
            });

            RuleFor(x => x.Geolocation)
                .NotNull()
                .WithMessage("A geolocalização deve ser informada.");

            When(x => x.Geolocation is not null, () =>
            {
                RuleFor(x => x.Geolocation.Latitude)
                    .InclusiveBetween(-90.0, 90.0)
                    .WithMessage("Latitude deve estar entre -90 e 90.");

                RuleFor(x => x.Geolocation.Longitude)
                    .InclusiveBetween(-180.0, 180.0)
                    .WithMessage("Longitude deve estar entre -180 e 180.");

                RuleFor(x => x.Geolocation)
                    .Must(geo => !(geo.Latitude == 0 && geo.Longitude == 0))
                    .WithMessage("Localização inválida. Latitude e longitude não podem ser ambas zero.");
            });

            RuleFor(x => x.Address)
                .NotNull()
                .WithMessage("O endereço deve ser informado.");

            When(x => x.Address is not null, () =>
            {
                RuleFor(x => x.Address.Street)
                    .NotEmpty()
                    .WithMessage("A rua deve ser informada.")
                    .MaximumLength(200)
                    .WithMessage("A rua deve ter no máximo 200 caracteres.");

                RuleFor(x => x.Address.Number)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("O número deve ser maior ou igual a zero.");

                RuleFor(x => x.Address.City)
                    .NotEmpty()
                    .WithMessage("A cidade deve ser informada.")
                    .MaximumLength(100)
                    .WithMessage("A cidade deve ter no máximo 100 caracteres.");

                RuleFor(x => x.Address.State)
                    .NotEmpty()
                    .WithMessage("O estado deve ser informado.")
                    .Length(2)
                    .WithMessage("O estado deve ter exatamente 2 caracteres (UF).")
                    .Matches("^[A-Za-z]{2}$")
                    .WithMessage("O estado deve conter apenas letras.");

                RuleFor(x => x.Address.ZipCode)
                    .NotEmpty()
                    .WithMessage("O CEP deve ser informado.")
                    .Must(BeValidZipCode)
                    .WithMessage("CEP inválido. Deve conter 8 dígitos numéricos.");
            });

            RuleFor(x => x.TimeZoneId)
                .NotEmpty()
                .WithMessage("O fuso horário deve ser informado.")
                .MaximumLength(100)
                .WithMessage("O fuso horário deve ter no máximo 100 caracteres.")
                .Must(BeValidTimeZone)
                .WithMessage("Fuso horário inválido.");

            RuleFor(x => x.StartWorkTime)
                .NotEmpty()
                .WithMessage("O horário de início do expediente deve ser informado.");

            RuleFor(x => x.EndWorkTime)
                .NotEmpty()
                .WithMessage("O horário de término do expediente deve ser informado.");

            RuleFor(x => x)
                .Must(HaveValidShiftDuration)
                .WithMessage("O turno de trabalho deve ter pelo menos 1 hora.");

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("O e-mail deve ser informado.")
                .EmailAddress()
                .WithMessage("E-mail inválido.")
                .MaximumLength(255)
                .WithMessage("O e-mail deve ter no máximo 255 caracteres.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .WithMessage("O telefone deve ser informado.")
                .MaximumLength(20)
                .WithMessage("O telefone deve ter no máximo 20 caracteres.")
                .Matches(@"^[\d\s\-\(\)\+]+$")
                .WithMessage("Telefone contém caracteres inválidos.");

            RuleFor(x => x.AllowedRadiusMeters)
                .GreaterThan(0)
                .WithMessage("O raio permitido deve ser maior que zero.")
                .LessThanOrEqualTo(10000)
                .WithMessage("O raio permitido deve ser no máximo 10.000 metros.");

            RuleFor(x => x.ToleranceMinutes)
                .GreaterThanOrEqualTo(0)
                .WithMessage("A tolerância deve ser maior ou igual a zero.")
                .LessThanOrEqualTo(60)
                .WithMessage("A tolerância deve ser no máximo 60 minutos.");
        }

        private static bool BeValidCnpjFormat(string? cnpj)
        {
            if (string.IsNullOrWhiteSpace(cnpj))
                return false;

            var digitsOnly = new string(cnpj.Where(char.IsDigit).ToArray());
            return digitsOnly.Length == 14;
        }

        private static bool BeValidCnpjChecksum(string? cnpj)
        {
            if (string.IsNullOrWhiteSpace(cnpj))
                return false;

            var digitsOnly = new string(cnpj.Where(char.IsDigit).ToArray());
            if (digitsOnly.Length != 14)
                return false;

            if (digitsOnly.Distinct().Count() == 1)
                return false;

            int[] multiplier1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
            int[] multiplier2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

            var tempCnpj = digitsOnly[..12];
            var sum = 0;

            for (int i = 0; i < 12; i++)
                sum += int.Parse(tempCnpj[i].ToString()) * multiplier1[i];

            var remainder = sum % 11;
            var digit1 = remainder < 2 ? 0 : 11 - remainder;

            tempCnpj += digit1;
            sum = 0;

            for (int i = 0; i < 13; i++)
                sum += int.Parse(tempCnpj[i].ToString()) * multiplier2[i];

            remainder = sum % 11;
            var digit2 = remainder < 2 ? 0 : 11 - remainder;

            return digitsOnly.EndsWith($"{digit1}{digit2}");
        }

        private static bool BeValidZipCode(string? zipCode)
        {
            if (string.IsNullOrWhiteSpace(zipCode))
                return false;

            var digitsOnly = new string(zipCode.Where(char.IsDigit).ToArray());
            return digitsOnly.Length == 8;
        }

        private static bool BeValidTimeZone(string? timeZoneId)
        {
            if (string.IsNullOrWhiteSpace(timeZoneId))
                return false;

            return ValidTimeZones.Contains(timeZoneId);
        }

        private static bool HaveValidShiftDuration(CreateBranchCommand command)
        {
            var duration = CalculateDuration(command.StartWorkTime, command.EndWorkTime);
            return duration.TotalHours >= 1;
        }

        private static TimeSpan CalculateDuration(TimeOnly start, TimeOnly end)
        {
            return start <= end
                ? end - start
                : (TimeSpan.FromHours(24) - start.ToTimeSpan()) + end.ToTimeSpan();
        }
    }
}
