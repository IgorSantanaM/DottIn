using DottIn.Application.Features.Branches.Queries.GetBranchByDocument;
using FluentValidation;

namespace DottIn.Application.Features.Branches.Validators
{
    public class GetBranchByDocumentQueryValidator : AbstractValidator<GetBranchByDocumentQuery>
    {
        public GetBranchByDocumentQueryValidator()
        {
            RuleFor(x => x.Document)
                .NotEmpty()
                .WithMessage("O documento (CNPJ) deve ser informado.")
                .Must(doc => !string.IsNullOrWhiteSpace(doc) && 
                             new string(doc.Where(char.IsDigit).ToArray()).Length == 14)
                .WithMessage("CNPJ inválido. Deve conter 14 dígitos numéricos.");
        }
    }
}
