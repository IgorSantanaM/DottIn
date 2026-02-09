using DottIn.Application.Exceptions;
using DottIn.Application.Features.Branches.DTOs;
using DottIn.Application.Shared.DTOS;
using DottIn.Domain.Branches;
using DottIn.Domain.Employees;
using FluentValidation;
using MediatR;

namespace DottIn.Application.Features.Branches.Queries.GetBranchByOwner
{
    public class GetBranchByOwnerQueryHandler(IValidator<GetBranchByOwnerQuery> validator, IBranchRepository branchRepository, IEmployeeRepository employeeRepository) : IRequestHandler<GetBranchByOwnerQuery, IEnumerable<BranchSummaryDto>>
    {
        public async Task<IEnumerable<BranchSummaryDto>> Handle(GetBranchByOwnerQuery request, CancellationToken cancellationToken)
        {
            await validator.ValidateAndThrowAsync(request, cancellationToken);

            var branches = await branchRepository.GetByOwnerIdAsync(request.OwnerId, cancellationToken);

            if (branches == null)
                throw NotFoundException.ForEntity(nameof(Branch), request.OwnerId);

            var employee = await employeeRepository.GetByIdAsync(request.OwnerId, cancellationToken);

            if (employee is null)
                throw NotFoundException.ForEntity(nameof(Employee), request.OwnerId);

            string ownerName = employee.Name;

            var result = new List<BranchSummaryDto>();

            foreach (var branch in branches)
            {
                var dto = new BranchSummaryDto(
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
                        ownerName);

                result.Add(dto);
            }

            return result;
        }
    }
}