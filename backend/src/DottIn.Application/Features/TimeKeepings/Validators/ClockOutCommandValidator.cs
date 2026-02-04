using DottIn.Application.Features.TimeKeepings.Commands.ClockOut;
using FluentValidation;

namespace DottIn.Application.Features.TimeKeepings.Validators
{
    public class ClockOutCommandValidator : AbstractValidator<ClockOutCommand>
    {
        public ClockOutCommandValidator()
        {
            RuleFor(co => co.BranchId)
                .NotEmpty()
                .WithMessage("O ID da empresa deve ser informado.");

            RuleFor(co => co.EmployeeId)
                .NotEmpty()
                .WithMessage("O ID do funcionário deve ser informado.");

            RuleFor(co => co.GeolocationDto)
                .NotNull()
                .WithMessage("A geolocalização deve ser informada.");

            When(co => co.GeolocationDto is not null, () =>
            {
                RuleFor(co => co.GeolocationDto.Latitude)
                    .InclusiveBetween(-90.0, 90.0)
                    .WithMessage("Latitude deve estar entre -90 e 90.");

                RuleFor(co => co.GeolocationDto.Longitude)
                    .InclusiveBetween(-180.0, 180.0)
                    .WithMessage("Longitude deve estar entre -180 e 180.");
            });
        }
    }
}
