using DottIn.Domain.Subscriptions;
using DottIn.Infra.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace DottIn.Infra.Data.Repositories
{
    public class TenantSubscriptionRepository : Repository<TenantSubscription, Guid>, ITenantSubscriptionRepository
    {
        public TenantSubscriptionRepository(DottInContext context) : base(context)
        {
        }

        public async Task<TenantSubscription?> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.OwnerId == ownerId, cancellationToken);
        }

        public async Task<TenantSubscription?> GetByHeadquartersIdAsync(Guid headquartersId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.HeadquartersId == headquartersId, cancellationToken);
        }

        public async Task<TenantSubscription?> GetByStripeCustomerIdAsync(string stripeCustomerId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.StripeCustomerId == stripeCustomerId, cancellationToken);
        }

        public async Task<TenantSubscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId, cancellationToken);
        }

        public async Task<bool> ExistsByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(s => s.OwnerId == ownerId, cancellationToken);
        }
    }
}
