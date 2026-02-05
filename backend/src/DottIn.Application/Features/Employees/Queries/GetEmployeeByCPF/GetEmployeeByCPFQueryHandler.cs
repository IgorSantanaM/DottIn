using DottIn.Application.Exceptions;
using DottIn.Application.Features.Employees.DTOs;
using DottIn.Application.Shared.DTOS;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Employees;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace DottIn.Application.Features.Employees.Queries.GetEmployeeByCPF
{
    public class GetEmployeeByCPFQueryHandler(IBranchRepository branchRepository, IEmployeeRepository employeeRepository) : IRequestHandler<GetEmployeeByCPFQuery, EmployeeSummaryDto>
    {
        public async Task<EmployeeSummaryDto> Handle(GetEmployeeByCPFQuery request, CancellationToken cancellationToken)
        {
            var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);

            if (branch is null)
                throw NotFoundException.ForEntity(nameof(Branch), request.BranchId);

            if (!branch.IsActive)
                throw new DomainException("A empresa não esta ativa.");

            var employee = await employeeRepository.GetByCPFAsync(request.CPF);

            if (employee is null)
                throw NotFoundException.ForEntity(nameof(Employee), request.CPF);

            var employeeSummaryDto = new EmployeeSummaryDto(employee.Id,
                        employee.Name,
                        new DocumentDTO(employee.CPF.Value, employee.CPF.Type),
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
