using DottIn.Application.Features.Branches.DTOs;
using MediatR;

namespace DottIn.Application.Features.Branches.Queries.GetActiveBranches
{
    public record GetActiveBranchesQuery() : IRequest<IEnumerable<BranchSummaryDto>>; // TODO: MULTI-TENANCY
}
