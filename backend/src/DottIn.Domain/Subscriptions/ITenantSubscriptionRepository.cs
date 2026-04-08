namespace DottIn.Domain.Subscriptions
{
    public interface ITenantSubscriptionRepository
    {
        Task<TenantSubscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<TenantSubscription?> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
        Task<TenantSubscription?> GetByHeadquartersIdAsync(Guid headquartersId, CancellationToken cancellationToken = default);
        Task<TenantSubscription?> GetByStripeCustomerIdAsync(string stripeCustomerId, CancellationToken cancellationToken = default);
        Task<TenantSubscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default);
        Task<bool> ExistsByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
        Task AddAsync(TenantSubscription subscription, CancellationToken cancellationToken = default);
        Task UpdateAsync(TenantSubscription subscription);
    }
}
