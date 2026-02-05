using DottIn.Application.Features.Employees.DTOs;
using MediatR;

namespace DottIn.Application.Features.Employees.Queries.GetEmployeeById;

public record GetEmployeeByIdQuery(Guid BranchId, Guid EmployeeId) : IRequest<EmployeeSummaryDto>;
