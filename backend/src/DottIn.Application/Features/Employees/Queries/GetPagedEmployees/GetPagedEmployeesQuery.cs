using DottIn.Application.Exceptions;
using DottIn.Application.Features.Employees.DTOs;
using DottIn.Application.Shared.DTOS;
using DottIn.Domain.Branches;
using DottIn.Domain.Common;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Employees;
using MediatR;

namespace DottIn.Application.Features.Employees.Queries.GetPagedEmployees;

public sealed record GetPagedEmployeesQuery(
    Guid BranchId,
    int PageNumber,
    int PageSize,
    string? Search,
    bool? IsActive) : IRequest<PagedResult<EmployeeSummaryDto>>;

public sealed class GetPagedEmployeesQueryHandler(
    IBranchRepository branchRepository,
    IEmployeeRepository employeeRepository)
    : IRequestHandler<GetPagedEmployeesQuery, PagedResult<EmployeeSummaryDto>>
{
    public async Task<PagedResult<EmployeeSummaryDto>> Handle(
        GetPagedEmployeesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.PageNumber < 1)
            throw new DomainException("O número da página deve ser maior que zero.");
        if (request.PageSize is < 10 or > 100)
            throw new DomainException("O tamanho da página deve estar entre 10 e 100 registros.");

        var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);
        if (branch is null)
            throw NotFoundException.ForEntity(nameof(Branch), request.BranchId);
        if (!branch.IsActive)
            throw new DomainException("A empresa não está ativa.");

        var (employees, totalCount) = await employeeRepository.GetPagedByBranchIdAsync(
            request.BranchId,
            request.PageNumber,
            request.PageSize,
            request.Search,
            request.IsActive,
            cancellationToken);

        var items = employees.Select(e => new EmployeeSummaryDto(
            e.Id,
            e.Name,
            new DocumentDto(e.CPF.Value, e.CPF.Type),
            e.ImageUrl,
            branch.Name,
            e.StartWorkTime,
            e.EndWorkTime,
            e.IntervalStart,
            e.IntervalEnd,
            e.IsActive,
            e.AllowOvernightShifts,
            !string.IsNullOrEmpty(e.FingerprintHash))).ToList();

        return new PagedResult<EmployeeSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };
    }
}