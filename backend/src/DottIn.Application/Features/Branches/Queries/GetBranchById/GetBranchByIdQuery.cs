using DottIn.Application.Features.Branches.DTOs;
using MediatR;

namespace DottIn.Application.Features.Branches.Queries.GetByBranchId;

public record GetBranchByIdQuery(Guid BranchId) : IRequest<BranchDetailsDto>;
