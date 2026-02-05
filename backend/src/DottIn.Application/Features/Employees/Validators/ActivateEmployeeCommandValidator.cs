using DottIn.Application.Features.Employees.Commands.ActivateEmployee;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace DottIn.Application.Features.Employees.Validators
{
    public class ActivateEmployeeCommandValidator : AbstractValidator<ActivateEmployeeCommand>
    {
        public ActivateEmployeeCommandValidator()
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
