using DottIn.Application.Features.Subscriptions.DTOs;

namespace DottIn.Application.Features.Subscriptions.Services
{
    public interface ITenantSubscriptionService
    {
        Task<TenantSubscriptionDto?> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
        Task<TenantSubscriptionDto?> GetByHeadquartersIdAsync(Guid headquartersId, CancellationToken cancellationToken = default);
        Task<bool> CanAddEmployeeAsync(Guid ownerId, CancellationToken cancellationToken = default);
        Task<bool> CanAddBranchAsync(Guid ownerId, CancellationToken cancellationToken = default);
        Task<int> GetEmployeeCountAsync(Guid ownerId, CancellationToken cancellationToken = default);
        Task<int> GetBranchCountAsync(Guid ownerId, CancellationToken cancellationToken = default);
    }
}
