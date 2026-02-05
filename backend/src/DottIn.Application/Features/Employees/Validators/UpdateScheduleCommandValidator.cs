using DottIn.Application.Features.Employees.Commands.UpdateSchedule;
using FluentValidation;

namespace DottIn.Application.Features.Employees.Validators
{
    public class UpdateScheduleCommandValidator : AbstractValidator<UpdateScheduleCommand>
    {
        public UpdateScheduleCommandValidator()
        {
            RuleFor(x => x.BranchId)
                .NotEmpty()
                .WithMessage("O ID da empresa deve ser informado.");

            RuleFor(x => x.EmployeeId)
                .NotEmpty()
                .WithMessage("O ID do funcionário deve ser informado.");

            RuleFor(x => x.Start)
                .NotEmpty()
                .WithMessage("O horário de início do expediente deve ser informado.");

            RuleFor(x => x.End)
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

        private static bool HaveValidShiftDuration(UpdateScheduleCommand command)
        {
            var duration = CalculateDuration(command.Start, command.End);
            return duration.TotalHours >= 1;
        }

        private static bool HaveValidIntervalDuration(UpdateScheduleCommand command)
        {
            var duration = CalculateDuration(command.IntervalStart, command.IntervalEnd);
            return duration.TotalMinutes >= 15;
        }

        private static bool HaveIntervalWithinShift(UpdateScheduleCommand command)
        {
            var isOvernight = command.Start > command.End;

            if (!isOvernight)
            {
                return command.IntervalStart >= command.Start &&
                       command.IntervalEnd <= command.End &&
                       command.IntervalStart < command.IntervalEnd;
            }

            var isIntervalInEvening = command.IntervalStart >= command.Start;
            var isIntervalInMorning = command.IntervalEnd <= command.End;
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
