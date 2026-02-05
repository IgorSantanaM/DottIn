using DottIn.Application.Features.Employees.DTOs;
using MediatR;

namespace DottIn.Application.Features.Employees.Queries.GetEmployeeByCPF;

public record GetEmployeeByCPFQuery(Guid BranchId, string CPF) : IRequest<EmployeeSummaryDto>;
