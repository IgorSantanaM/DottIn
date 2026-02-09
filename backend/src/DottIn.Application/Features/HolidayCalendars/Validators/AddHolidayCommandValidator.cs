using DottIn.Application.Features.HolidayCalendars.Commands.AddHoliday;
using DottIn.Domain.HolidayCalendars;
using FluentValidation;

namespace DottIn.Application.Features.HolidayCalendars.Validators
{
    public class AddHolidayCommandValidator : AbstractValidator<AddHolidayCommand>
    {
        public AddHolidayCommandValidator()
        {
            RuleFor(x => x.HolidayCalendarId)
                .NotEmpty()
                .WithMessage("O ID do calendário deve ser informado.");

            RuleFor(x => x.BranchId)
                .NotEmpty()
                .WithMessage("O ID da filial deve ser informado.");

            RuleFor(x => x.Holidays)
                .NotNull()
                .WithMessage("A lista de feriados deve ser informada.")
                .NotEmpty()
                .WithMessage("A lista de feriados não pode estar vazia.");

            RuleForEach(x => x.Holidays)
                .Must(h => h.Date != default)
                .WithMessage("A data do feriado deve ser informada.")
                .Must(h => !string.IsNullOrWhiteSpace(h.Name))
                .WithMessage("O nome do feriado deve ser informado.")
                .Must(h => h.Name.Length <= 100)
                .WithMessage("O nome do feriado deve ter no máximo 100 caracteres.")
                .Must(h => Enum.IsDefined(typeof(HolidayType), h.Type))
                .WithMessage("O tipo do feriado é inválido.");
        }
    }
}
