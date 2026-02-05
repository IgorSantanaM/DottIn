using DottIn.Application.Features.Employees.Commands.CreateEmployee;
using DottIn.Domain.ValueObjects;
using FluentValidation;

namespace DottIn.Application.Features.Employees.Validators
{
    public class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
    {
        private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/jpg"];

        public CreateEmployeeCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("O nome do funcionário deve ser informado.")
                .MaximumLength(150)
                .WithMessage("O nome do funcionário deve ter no máximo 150 caracteres.");

            RuleFor(x => x.Document)
                .NotNull()
                .WithMessage("O documento deve ser informado.");

            When(x => x.Document is not null, () =>
            {
                RuleFor(x => x.Document.Value)
                    .NotEmpty()
                    .WithMessage("O número do documento deve ser informado.")
                    .Must(BeValidCpf)
                    .WithMessage("CPF inválido. Deve conter 11 dígitos numéricos.");

                RuleFor(x => x.Document.Type)
                    .Must(type => type == DocumentType.CPF)
                    .WithMessage("Funcionário deve ser registrado com um CPF.");
            });

            RuleFor(x => x.ImageStream)
                .NotNull()
                .WithMessage("A imagem do funcionário deve ser informada.")
                .Must(stream => stream is not null && stream.Length > 0)
                .WithMessage("A imagem não pode estar vazia.");

            RuleFor(x => x.ImageContentType)
                .NotEmpty()
                .WithMessage("O tipo da imagem deve ser informado.")
                .Must(contentType => AllowedImageTypes.Contains(contentType.ToLowerInvariant()))
                .WithMessage("Tipo de imagem inválido. Apenas JPEG e PNG são permitidos.");

            RuleFor(x => x.BranchId)
                .NotEmpty()
                .WithMessage("O ID da empresa deve ser informado.");

            RuleFor(x => x.StartWorkTime)
                .NotEmpty()
                .WithMessage("O horário de início do expediente deve ser informado.");

            RuleFor(x => x.EndWorkTime)
                .NotEmpty()
                .WithMessage("O horário de término do expediente deve ser informado.");

            RuleFor(x => x.IntervalStart)
                .NotEmpty()
                .WithMessage("O horário de início do intervalo deve ser informado.");

            RuleFor(x => x.IntervalEnd)
                .NotEmpty()
                .WithMessage("O horário de término do intervalo deve ser informado.");

            RuleFor(x => x)
                .Must(HaveValidShiftDuration)
                .WithMessage("O turno de trabalho deve ter pelo menos 1 hora.");

            RuleFor(x => x)
                .Must(HaveValidIntervalDuration)
                .WithMessage("O intervalo deve ter no mínimo 15 minutos.");

            RuleFor(x => x)
                .Must(HaveIntervalWithinShift)
                .WithMessage("O horário de intervalo deve estar dentro do horário de expediente.");
        }

        private static bool BeValidCpf(string? cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
                return false;

            var digitsOnly = new string(cpf.Where(char.IsDigit).ToArray());
            return digitsOnly.Length == 11;
        }

        private static bool HaveValidShiftDuration(CreateEmployeeCommand command)
        {
            var duration = CalculateDuration(command.StartWorkTime, command.EndWorkTime);
            return duration.TotalHours >= 1;
        }

        private static bool HaveValidIntervalDuration(CreateEmployeeCommand command)
        {
            var duration = CalculateDuration(command.IntervalStart, command.IntervalEnd);
            return duration.TotalMinutes >= 15;
        }

        private static bool HaveIntervalWithinShift(CreateEmployeeCommand command)
        {
            var isOvernight = command.StartWorkTime > command.EndWorkTime;

            if (!isOvernight)
            {
                return command.IntervalStart >= command.StartWorkTime &&
                       command.IntervalEnd <= command.EndWorkTime &&
                       command.IntervalStart < command.IntervalEnd;
            }

            var isIntervalInEvening = command.IntervalStart >= command.StartWorkTime;
            var isIntervalInMorning = command.IntervalEnd <= command.EndWorkTime;
            var isIntervalOvernight = command.IntervalStart > command.IntervalEnd;

            return isIntervalOvernight
                ? isIntervalInEvening && isIntervalInMorning
                : isIntervalInEvening || isIntervalInMorning;
        }

        private static TimeSpan CalculateDuration(TimeOnly start, TimeOnly end)
        {
            return start <= end
                ? end - start
                : (TimeSpan.FromHours(24) - start.ToTimeSpan()) + end.ToTimeSpan();
        }
    }
}
