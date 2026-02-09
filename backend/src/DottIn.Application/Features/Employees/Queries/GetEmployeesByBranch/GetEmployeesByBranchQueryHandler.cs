using DottIn.Application.Exceptions;
using DottIn.Application.Features.Employees.DTOs;
using DottIn.Application.Shared.DTOS;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Employees;
using MediatR;

namespace DottIn.Application.Features.Employees.Queries.GetEmployeesByBranch
{
    public class GetEmployeesByBranchQueryHandler(IBranchRepository branchRepository, IEmployeeRepository employeeRepository) : IRequestHandler<GetEmployeesByBranchQuery, IEnumerable<EmployeeSummaryDto>>
    {
        public async Task<IEnumerable<EmployeeSummaryDto>> Handle(GetEmployeesByBranchQuery request, CancellationToken cancellationToken)
        {
            var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);

            if (branch is null)
                throw NotFoundException.ForEntity(nameof(Branch), request.BranchId);

            if (!branch.IsActive)
                throw new DomainException("A empresa não esta ativa.");

            var employees = await employeeRepository.GetByBranchIdAsync(request.BranchId, cancellationToken);

            var employeesSummaryDto = employees.Select(e =>
            new EmployeeSummaryDto(e.Id,
                        e.Name,
                        new DocumentDto(e.CPF.Value, e.CPF.Type),
                        e.ImageUrl,
                        branch.Name,
                        e.StartWorkTime,
                        e.EndWorkTime,
                        e.IntervalStart,
                        e.IntervalEnd,
                        e.IsActive,
                        e.AllowOvernightShifts));

            return employeesSummaryDto;
        }
    }
}
