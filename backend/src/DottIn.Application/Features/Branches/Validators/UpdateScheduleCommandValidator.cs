using DottIn.Application.Features.Branches.Commands.UpdateSchedule;
using FluentValidation;

namespace DottIn.Application.Features.Branches.Validators
{
    public class UpdateScheduleCommandValidator : AbstractValidator<UpdateScheduleCommand>
    {
        public UpdateScheduleCommandValidator()
        {
            RuleFor(x => x.BranchId)
                .NotEmpty()
                .WithMessage("O ID da empresa deve ser informado.");

            RuleFor(x => x.Start)
                .NotEmpty()
                .WithMessage("O horário de início do expediente deve ser informado.");

            RuleFor(x => x.End)
                .NotEmpty()
                .WithMessage("O horário de término do expediente deve ser informado.");

            RuleFor(x => x)
                .Must(HaveValidShiftDuration)
                .WithMessage("O turno de trabalho deve ter pelo menos 1 hora.");
        }

        private static bool HaveValidShiftDuration(UpdateScheduleCommand command)
        {
            var duration = CalculateDuration(command.Start, command.End);
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
