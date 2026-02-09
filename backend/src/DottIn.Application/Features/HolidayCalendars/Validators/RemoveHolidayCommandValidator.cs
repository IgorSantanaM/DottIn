using DottIn.Application.Features.HolidayCalendars.Commands.RemoveHoliday;
using FluentValidation;

namespace DottIn.Application.Features.HolidayCalendars.Validators
{
    public class RemoveHolidayCommandValidator : AbstractValidator<RemoveHolidayCommand>
    {
        public RemoveHolidayCommandValidator()
        {
            RuleFor(x => x.HolidayCalendarId)
                .NotEmpty()
                .WithMessage("O ID do calendário deve ser informado.");

            RuleFor(x => x.BranchId)
                .NotEmpty()
                .WithMessage("O ID da filial deve ser informado.");

            RuleFor(x => x.Date)
                .NotEmpty()
                .WithMessage("A data do feriado deve ser informada.")
                .Must(date => date != default)
                .WithMessage("A data do feriado é inválida.");
        }
    }
}
