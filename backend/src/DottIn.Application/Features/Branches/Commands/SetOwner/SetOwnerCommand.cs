using MediatR;

namespace DottIn.Application.Features.Branches.Commands.SetOwner;

public record SetOwnerCommand(Guid BranchId, Guid EmployeeId) : IRequest<Unit>;
