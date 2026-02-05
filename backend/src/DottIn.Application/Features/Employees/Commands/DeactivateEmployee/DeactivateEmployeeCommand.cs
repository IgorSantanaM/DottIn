using MediatR;

namespace DottIn.Application.Features.Employees.Commands.DeactivateEmployee;

public record DeactivateEmployeeCommand(Guid EmployeeId, Guid BranchId) : IRequest<bool>;
