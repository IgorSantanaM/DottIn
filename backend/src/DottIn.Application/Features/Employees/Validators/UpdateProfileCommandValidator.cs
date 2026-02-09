using DottIn.Application.Features.Employees.Commands.UpdateProfile;
using FluentValidation;

namespace DottIn.Application.Features.Employees.Validators
{
    public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
    {
        private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/jpg"];

        public UpdateProfileCommandValidator()
        {
            RuleFor(x => x.EmployeeId)
                .NotEmpty()
                .WithMessage("O ID do funcionário deve ser informado.");

            RuleFor(x => x.BranchId)
                .NotEmpty()
                .WithMessage("O ID da empresa deve ser informado.");

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("O nome deve ser informado.")
                .MaximumLength(150)
                .WithMessage("O nome deve ter no máximo 150 caracteres.");

            When(x => x.EmployeeImage is not null, () =>
            {
                RuleFor(x => x.EmployeeImage)
                    .Must(stream => stream!.Length > 0)
                    .WithMessage("A imagem não pode estar vazia.");

                RuleFor(x => x.ImageContentType)
                    .NotEmpty()
                    .WithMessage("O tipo da imagem deve ser informado quando uma imagem é enviada.")
                    .Must(contentType => !string.IsNullOrEmpty(contentType) &&
                                         AllowedImageTypes.Contains(contentType.ToLowerInvariant()))
                    .WithMessage("Tipo de imagem inválido. Apenas JPEG e PNG são permitidos.");
            });
        }
    }
}
