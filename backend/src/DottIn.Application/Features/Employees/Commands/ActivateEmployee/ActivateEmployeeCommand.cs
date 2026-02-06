using MediatR;

namespace DottIn.Application.Features.Employees.Commands.ActivateEmployee;

public record ActivateEmployeeCommand(Guid EmployeeId, Guid BranchId) : IRequest<Unit>;
