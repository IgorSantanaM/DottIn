using DottIn.Application.Features.Auth.DTOs;
using FluentValidation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace DottIn.Application.Features.Auth.Validators
{
    public class LoginRequestValidator : AbstractValidator<LoginRequest>    
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.CompanyCode)
                .NotEmpty()
                .WithMessage("O código da empresa deve ser informado.");

            RuleFor(x => x.Cpf)
                .NotEmpty()
                .WithMessage("O CPF deve ser informado.")
                .Matches(@"^\d{11}$")
                .WithMessage("O CPF deve conter 11 dígitos numéricos.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Informe a senha.")
                .MinimumLength(6)
                .WithMessage("A senha deve conter pelo menos 6 caracteres.");
        }
    }
}
