using DottIn.Application.Features.HolidayCalendars.Commands.UpdateHoliday;
using DottIn.Domain.HolidayCalendars;
using FluentValidation;

namespace DottIn.Application.Features.HolidayCalendars.Validators
{
    public class UpdateHolidayCommandValidator : AbstractValidator<UpdateHolidayCommand>
    {
        public UpdateHolidayCommandValidator()
        {
            RuleFor(x => x.HolidayCalendarId)
                .NotEmpty()
                .WithMessage("O ID do calendário deve ser informado.");

            RuleFor(x => x.BranchId)
                .NotEmpty()
                .WithMessage("O ID da filial deve ser informado.");

            RuleFor(x => x.NewDate)
                .NotEmpty()
                .WithMessage("A data do feriado deve ser informada.")
                .Must(date => date != default)
                .WithMessage("A data do feriado é inválida.");

            When(x => !string.IsNullOrEmpty(x.NewName), () =>
            {
                RuleFor(x => x.NewName)
                    .MaximumLength(100)
                    .WithMessage("O nome do feriado deve ter no máximo 100 caracteres.");
            });

            When(x => x.NewType.HasValue, () =>
            {
                RuleFor(x => x.NewType)
                    .Must(type => type.HasValue && Enum.IsDefined(typeof(HolidayType), type.Value))
                    .WithMessage("O tipo do feriado é inválido.");
            });

            RuleFor(x => x)
                .Must(cmd => !string.IsNullOrEmpty(cmd.NewName) || 
                             cmd.NewType.HasValue || 
                             cmd.IsOptional.HasValue)
                .WithMessage("Pelo menos um campo deve ser informado para atualização.");
        }
    }
}
