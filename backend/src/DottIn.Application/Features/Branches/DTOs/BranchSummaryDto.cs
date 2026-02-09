using DottIn.Application.Shared.DTOS;

namespace DottIn.Application.Features.Branches.DTOs
{
    public record BranchSummaryDto(string Name,
                    DocumentDto Document,
                    string? Email,
                    string? PhoneNumber,
                    AddressDto Address,
                    TimeOnly StartWork,
                    TimeOnly EndWork,
                    bool IsActive,
                    bool IsHeadquarters,
                    string OwnerName);
}
