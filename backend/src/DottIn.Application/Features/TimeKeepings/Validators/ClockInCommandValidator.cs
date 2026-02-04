using DottIn.Application.Features.TimeKeepings.Commands.ClockIn;
using FluentValidation;

namespace DottIn.Application.Features.TimeKeepings.Validators
{
    public class ClockInCommandValidator : AbstractValidator<ClockInCommand>
    {
        public ClockInCommandValidator()
        {
            RuleFor(ci => ci.BranchId)
                .NotEmpty()
                .WithMessage("O ID da empresa deve ser informado.");

            RuleFor(ci => ci.EmployeeId)
                .NotEmpty()
                .WithMessage("O ID do funcionário deve ser informado.");

            RuleFor(ci => ci.GeolocationDto)
                .NotNull()
                .WithMessage("A geolocalização deve ser informada.");

            When(ci => ci.GeolocationDto is not null, () =>
            {
                RuleFor(ci => ci.GeolocationDto.Latitude)
                    .InclusiveBetween(-90.0, 90.0)
                    .WithMessage("Latitude deve estar entre -90 e 90.");

                RuleFor(ci => ci.GeolocationDto.Longitude)
                    .InclusiveBetween(-180.0, 180.0)
                    .WithMessage("Longitude deve estar entre -180 e 180.");
            });
        }
    }
}
