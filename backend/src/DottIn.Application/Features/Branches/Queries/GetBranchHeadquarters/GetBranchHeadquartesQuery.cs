using DottIn.Application.Features.Branches.DTOs;
using MediatR;

namespace DottIn.Application.Features.Branches.Queries.GetBranchHeadquarters;

public record GetBranchHeadquartesQuery : IRequest<IEnumerable<BranchSummaryDto>>;
