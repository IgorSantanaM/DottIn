using DottIn.Application.Features.Branches.Queries.GetBranchByOwner;
using FluentValidation;

namespace DottIn.Application.Features.Branches.Validators
{
    public class GetBranchByOwnerQueryValidator : AbstractValidator<GetBranchByOwnerQuery>
    {
        public GetBranchByOwnerQueryValidator()
        {
            RuleFor(x => x.OwnerId)
                .NotEmpty()
                .WithMessage("O ID do proprietário deve ser informado.");
        }
    }
}
