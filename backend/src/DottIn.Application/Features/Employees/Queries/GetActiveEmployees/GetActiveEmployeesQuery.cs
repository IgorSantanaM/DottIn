using DottIn.Application.Features.Employees.DTOs;
using MediatR;

namespace DottIn.Application.Features.Employees.Queries.GetActiveEmployees;

public record GetActiveEmployeesQuery(Guid BranchId) : IRequest<IEnumerable<EmployeeSummaryDto>>;
