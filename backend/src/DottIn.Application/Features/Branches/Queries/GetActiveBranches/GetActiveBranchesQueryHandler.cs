using DottIn.Application.Features.Branches.DTOs;
using DottIn.Application.Shared.DTOS;
using DottIn.Domain.Branches;
using DottIn.Domain.Employees;
using MediatR;

namespace DottIn.Application.Features.Branches.Queries.GetActiveBranches
{
    public class GetActiveBranchesQueryHandler(
        IBranchRepository branchRepository,
        IEmployeeRepository employeeRepository) : IRequestHandler<GetActiveBranchesQuery, IEnumerable<BranchSummaryDto>>
    {
        public async Task<IEnumerable<BranchSummaryDto>> Handle(
            GetActiveBranchesQuery request,
            CancellationToken cancellationToken)
        {
            var branches = await branchRepository.GetActiveBranchesAsync(cancellationToken);

            var result = new List<BranchSummaryDto>();

            foreach (var branch in branches)
            {
                var ownerName = string.Empty;

                if (branch.OwnerId != Guid.Empty)
                {
                    var owner = await employeeRepository.GetByIdAsync(branch.OwnerId);
                    ownerName = owner?.Name ?? string.Empty;
                }

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
