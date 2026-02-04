using DottIn.Application.Exceptions;
using DottIn.Application.Features.TimeKeepings.DTOs;
using DottIn.Application.Features.TimeKeepings.Queries.GetAllTimeKeepings;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Employees;
using DottIn.Domain.TimeKeepings;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace DottIn.Application.Features.TimeKeepings.Queries.GetByEmployeeAndPeriod
{
    public class GetTimeKeepingByPeriodQueryHandler(ITimeKeepingRepository timeKeepingRepository,
        IEmployeeRepository employeeRepository,
        IBranchRepository branchRepository) 
        : IRequestHandler<GetTimeKeepingByPeriodQuery, IEnumerable<TimeKeepingSummaryDto>>
    {
        public async Task<IEnumerable<TimeKeepingSummaryDto>> Handle(GetTimeKeepingByPeriodQuery request, CancellationToken cancellationToken)
        {
            var employee = await employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);

            if (employee is null)
                throw NotFoundException.ForEntity(nameof(Employee), request.EmployeeId);

            if (!employee.IsActive)
                throw new DomainException("O Funcionário não esta ativo.");

            var branch = await branchRepository.GetByIdAsync(employee.BranchId, cancellationToken);

            if(branch is null)
                throw NotFoundException.ForEntity(nameof(Branch), employee.BranchId);

            if(!branch.IsActive)
                throw new DomainException("A Empresa não esta ativa.");

            var timeKeepings = await timeKeepingRepository
                .GetByEmployeeAndPeriodAsync(request.EmployeeId, request.StartDate, request.EndDate);

            var timeKeepingsSummary = timeKeepings.Select(tk => new TimeKeepingSummaryDto(tk.Status, tk.CreatedAt, tk.Entries.Count));

            return timeKeepingsSummary;
        }
    }
}
