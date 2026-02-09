using DottIn.Application.Features.Employees.Commands.DeactivateEmployee;
using FluentValidation;

namespace DottIn.Application.Features.Employees.Validators
{
    public class DeactivateEmployeeCommandValidator : AbstractValidator<DeactivateEmployeeCommand>
    {
        public DeactivateEmployeeCommandValidator()
        {
            RuleFor(x => x.EmployeeId)
               .NotEmpty()
               .WithMessage("O ID do funcionário deve ser informado.");

            RuleFor(x => x.BranchId)
                .NotEmpty()
                .WithMessage("O ID da empresa deve ser informado.");
        }
    }
}
