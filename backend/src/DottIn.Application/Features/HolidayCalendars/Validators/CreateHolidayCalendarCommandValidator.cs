using DottIn.Application.Features.HolidayCalendars.Commands.CreateHolidayCalendar;
using FluentValidation;

namespace DottIn.Application.Features.HolidayCalendars.Validators
{
    public class CreateHolidayCalendarCommandValidator : AbstractValidator<CreateHolidayCalendarCommand>
    {
        public CreateHolidayCalendarCommandValidator()
        {
            RuleFor(x => x.BranchId)
                .NotEmpty()
                .WithMessage("O ID da filial deve ser informado.");

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("O nome do calendário deve ser informado.")
                .MaximumLength(100)
                .WithMessage("O nome do calendário deve ter no máximo 100 caracteres.");

            RuleFor(x => x.CountryCode)
                .NotEmpty()
                .WithMessage("O código do país deve ser informado.")
                .Length(2)
                .WithMessage("O código do país deve ter exatamente 2 caracteres (ISO 3166-1 alpha-2).")
                .Matches("^[A-Za-z]{2}$")
                .WithMessage("O código do país deve conter apenas letras.");

            RuleFor(x => x.Year)
                .GreaterThanOrEqualTo(DateTime.UtcNow.Year - 1)
                .WithMessage($"O ano deve ser maior ou igual a {DateTime.UtcNow.Year - 1}.")
                .LessThanOrEqualTo(DateTime.UtcNow.Year + 5)
                .WithMessage($"O ano deve ser menor ou igual a {DateTime.UtcNow.Year + 5}.");

            When(x => !string.IsNullOrEmpty(x.RegionCode), () =>
            {
                RuleFor(x => x.RegionCode)
                    .MaximumLength(10)
                    .WithMessage("O código da região deve ter no máximo 10 caracteres.")
                    .Matches("^[A-Za-z0-9]+$")
                    .WithMessage("O código da região deve conter apenas letras e números.");
            });

            When(x => !string.IsNullOrEmpty(x.Description), () =>
            {
                RuleFor(x => x.Description)
                    .MaximumLength(500)
                    .WithMessage("A descrição deve ter no máximo 500 caracteres.");
            });
        }
    }
}
