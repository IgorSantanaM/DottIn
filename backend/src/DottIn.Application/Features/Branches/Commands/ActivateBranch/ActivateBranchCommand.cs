using MediatR;

namespace DottIn.Application.Features.Branches.Commands.ActivateBranch;

public record ActivateBranchCommand(Guid BranchId) : IRequest<Unit>;
