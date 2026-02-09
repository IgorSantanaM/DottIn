using DottIn.Application.Exceptions;
using DottIn.Application.Features.Employees.DTOs;
using DottIn.Application.Shared.DTOS;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Employees;
using MediatR;

namespace DottIn.Application.Features.Employees.Queries.GetEmployeeById
{
    public class GetEmployeeByIdQueryHandler(IBranchRepository branchRepository,
        IEmployeeRepository employeeRepository)
        : IRequestHandler<GetEmployeeByIdQuery, EmployeeSummaryDto>
    {
        public async Task<EmployeeSummaryDto> Handle(GetEmployeeByIdQuery request, CancellationToken cancellationToken)
        {
            var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);

            if (branch is null)
                throw NotFoundException.ForEntity(nameof(Branch), request.BranchId);

            if (!branch.IsActive)
                throw new DomainException("A empresa não esta ativa.");

            var employee = await employeeRepository.GetByIdAsync(request.EmployeeId);

            if (employee is null)
                throw NotFoundException.ForEntity(nameof(Employee), request.EmployeeId);

            var employeeSummaryDto = new EmployeeSummaryDto(employee.Id,
                        employee.Name,
                        new DocumentDto(employee.CPF.Value, employee.CPF.Type),
                        employee.ImageUrl,
                        branch.Name,
                        employee.StartWorkTime,
                        employee.EndWorkTime,
                        employee.IntervalStart,
                        employee.IntervalEnd,
                        employee.IsActive,
                        employee.AllowOvernightShifts);

            return employeeSummaryDto;
        }
    }
}
