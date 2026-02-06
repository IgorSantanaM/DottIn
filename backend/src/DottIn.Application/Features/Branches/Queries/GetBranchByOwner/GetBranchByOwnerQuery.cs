using DottIn.Application.Features.Branches.DTOs;
using MediatR;

namespace DottIn.Application.Features.Branches.Queries.GetBranchByOwner;

public record GetBranchByOwnerQuery(Guid OwnerId) : IRequest<IEnumerable<BranchSummaryDto>>;
