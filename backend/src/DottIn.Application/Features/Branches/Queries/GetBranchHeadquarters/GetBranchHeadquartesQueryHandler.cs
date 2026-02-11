using DottIn.Application.Exceptions;
using DottIn.Application.Features.Branches.DTOs;
using DottIn.Application.Shared.DTOS;
using DottIn.Domain.Branches;
using DottIn.Domain.Employees;
using MediatR;

namespace DottIn.Application.Features.Branches.Queries.GetBranchHeadquarters
{
    public class GetBranchHeadquartesQueryHandler(IBranchRepository branchRepository, IEmployeeRepository employeeRepository) : IRequestHandler<GetBranchHeadquartesQuery, IEnumerable<BranchSummaryDto>>
    {
        public async Task<IEnumerable<BranchSummaryDto>> Handle(GetBranchHeadquartesQuery request, CancellationToken cancellationToken)
        {
            var branches = await branchRepository.GetHeadquartersAsync(cancellationToken);

            if (branches == null)
                throw new NotFoundException("Não existem empresas matrizes.");

            var result = new List<BranchSummaryDto>();

            foreach (var branch in branches)
            {
                var ownerName = string.Empty;

                if (branch.OwnerId != Guid.Empty && branch.OwnerId is not null)
                {
                    var owner = await employeeRepository.GetByIdAsync((Guid)branch.OwnerId);
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
