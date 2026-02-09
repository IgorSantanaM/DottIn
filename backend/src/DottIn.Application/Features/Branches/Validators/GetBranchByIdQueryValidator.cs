using DottIn.Application.Features.Branches.Queries.GetBranchById;
using FluentValidation;

namespace DottIn.Application.Features.Branches.Validators
{
    public class GetBranchByIdQueryValidator : AbstractValidator<GetBranchByIdQuery>
    {
        public GetBranchByIdQueryValidator()
        {
            RuleFor(x => x.BranchId)
                .NotEmpty()
                .WithMessage("O ID da empresa deve ser informado.");
        }
    }
}
