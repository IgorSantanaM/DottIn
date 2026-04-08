namespace DottIn.Domain.Subscriptions
{
    public interface ISubscriptionPlanRepository
    {
        Task<SubscriptionPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<SubscriptionPlan?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
        Task<SubscriptionPlan?> GetByStripePriceIdAsync(string stripePriceId, CancellationToken cancellationToken = default);
        Task<SubscriptionPlan?> GetFreePlanAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<SubscriptionPlan>> GetAllActiveAsync(CancellationToken cancellationToken = default);
        Task AddAsync(SubscriptionPlan plan, CancellationToken cancellationToken = default);
        Task UpdateAsync(SubscriptionPlan plan);
    }
}
