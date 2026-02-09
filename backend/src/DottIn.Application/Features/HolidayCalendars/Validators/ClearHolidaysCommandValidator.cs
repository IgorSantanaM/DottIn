using DottIn.Application.Features.HolidayCalendars.Commands.ClearHolidays;
using FluentValidation;

namespace DottIn.Application.Features.HolidayCalendars.Validators
{
    public class ClearHolidaysCommandValidator : AbstractValidator<ClearHolidaysCommand>
    {
        public ClearHolidaysCommandValidator()
        {
            RuleFor(x => x.HolidayCalendarId)
                .NotEmpty()
                .WithMessage("O ID do calendário deve ser informado.");

            RuleFor(x => x.BranchId)
                .NotEmpty()
                .WithMessage("O ID da filial deve ser informado.");
        }
    }
}
