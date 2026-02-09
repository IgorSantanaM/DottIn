using DottIn.Application.Features.Branches.DTOs;
using MediatR;

namespace DottIn.Application.Features.Branches.Queries.GetBranchById;

public record GetBranchByIdQuery(Guid BranchId) : IRequest<BranchDetailsDto>;
