using DottIn.Application.Features.TimeKeepings.Commands.Break;
using FluentValidation;

namespace DottIn.Application.Features.TimeKeepings.Validators
{
    public class BreakCommandValidator : AbstractValidator<BreakCommand>
    {
        public BreakCommandValidator()
        {
            RuleFor(b => b.EmployeeId)
                .NotEmpty()
                .WithMessage("O ID do funcionário deve ser informado.");

            RuleFor(b => b.BranchId)
                .NotEmpty()
                .WithMessage("O ID da empresa deve ser informado.");

            RuleFor(b => b.GeolocationDto)
                .NotNull()
                .WithMessage("A geolocalização deve ser informada.");

            When(b => b.GeolocationDto is not null, () =>
            {
                RuleFor(b => b.GeolocationDto.Latitude)
                    .InclusiveBetween(-90.0, 90.0)
                    .WithMessage("Latitude deve estar entre -90 e 90.");

                RuleFor(b => b.GeolocationDto.Longitude)
                    .InclusiveBetween(-180.0, 180.0)
                    .WithMessage("Longitude deve estar entre -180 e 180.");
            });
        }
    }
}
