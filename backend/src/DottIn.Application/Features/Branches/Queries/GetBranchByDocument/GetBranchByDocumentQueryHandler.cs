using DottIn.Application.Exceptions;
using DottIn.Application.Features.Branches.DTOs;
using DottIn.Application.Shared.DTOS;
using DottIn.Domain.Branches;
using DottIn.Domain.Employees;
using FluentValidation;
using MediatR;

namespace DottIn.Application.Features.Branches.Queries.GetBranchByDocument
{
    public class GetBranchByDocumentQueryHandler(IValidator<GetBranchByDocumentQuery> validator,
        IBranchRepository branchRepository,
        IEmployeeRepository employeeRepository)
        : IRequestHandler<GetBranchByDocumentQuery, BranchDetailsDto>
    {
        public async Task<BranchDetailsDto> Handle(GetBranchByDocumentQuery request, CancellationToken cancellationToken)
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);

            var branch = await branchRepository.GetByDocumentAsync(request.Document, cancellationToken);

            if (branch is null)
                throw NotFoundException.ForEntity(nameof(Branch), request.Document);

            var employee = await employeeRepository.GetByIdAsync(branch.OwnerId, cancellationToken);

            if (employee is null)
                throw NotFoundException.ForEntity(nameof(Employee), branch.OwnerId);

            string ownerName = employee.Name;

            var result = new BranchDetailsDto(
                    branch.Name,
                    new DocumentDto(branch.Document.Value, branch.Document.Type),
                    branch.Email,
                    branch.PhoneNumber,
                    new AddressDto(
                        branch.Address.Street,
                        branch.Address.Number,
                        branch.Address.Complement,
                        branch.Address.City,
                        branch.Address.State,
                        branch.Address.ZipCode),
                    branch.StartWorkTime,
                    branch.EndWorkTime,
                    branch.IsActive,
                    branch.IsHeadquarters,
                    ownerName,
                    branch.AllowOvernightShifts,
                    branch.HolidayCalendarId,
                    branch.ToleranceMinutes,
                    branch.AllowedRadiusMeters,
                    branch.CreatedAt,
                    branch.UpdatedAt);

            return result;
        }
    }
}
