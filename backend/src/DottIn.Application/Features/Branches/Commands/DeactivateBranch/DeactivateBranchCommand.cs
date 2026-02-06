using MediatR;

namespace DottIn.Application.Features.Branches.Commands.DeactivateBranch;

public record DeactivateBranchCommand(Guid BranchId) : IRequest<Unit>;
