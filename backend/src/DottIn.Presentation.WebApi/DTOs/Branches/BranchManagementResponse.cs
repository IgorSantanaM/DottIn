using DottIn.Application.Features.Subscriptions.DTOs;

namespace DottIn.Presentation.WebApi.DTOs.Branches;

public record BranchManagementResponse(Guid OwnerId, IReadOnlyList<ManagedBranchDto> Branches, TenantSubscriptionDto? Subscription);

public record ManagedBranchDto(Guid Id, string Name, string Document, string CompanyCode,
    bool IsActive, bool IsHeadquarters, int ActiveEmployeeCount, string TimeZoneId,
    TimeOnly StartWork, TimeOnly EndWork, int AllowedRadiusMeters, int ToleranceMinutes);
