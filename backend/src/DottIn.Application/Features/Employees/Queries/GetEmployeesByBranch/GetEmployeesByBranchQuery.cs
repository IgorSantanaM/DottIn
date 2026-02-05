using DottIn.Application.Features.Employees.DTOs;
using MediatR;

namespace DottIn.Application.Features.Employees.Queries.GetEmployeesByBranch;

public record GetEmployeesByBranchQuery(Guid BranchId) : IRequest<IEnumerable<EmployeeSummaryDto>>;
