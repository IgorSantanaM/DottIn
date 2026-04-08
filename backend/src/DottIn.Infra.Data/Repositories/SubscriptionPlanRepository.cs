using DottIn.Domain.Subscriptions;
using DottIn.Infra.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace DottIn.Infra.Data.Repositories
{
    public class SubscriptionPlanRepository : Repository<SubscriptionPlan, Guid>, ISubscriptionPlanRepository
    {
        public SubscriptionPlanRepository(DottInContext context) : base(context)
        {
        }

        public async Task<SubscriptionPlan?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(p => p.Name == name, cancellationToken);
        }

        public async Task<SubscriptionPlan?> GetByStripePriceIdAsync(string stripePriceId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(p => p.StripePriceId == stripePriceId, cancellationToken);
        }

        public async Task<SubscriptionPlan?> GetFreePlanAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(p => p.Name == "Free" && p.IsActive, cancellationToken);
        }

        public async Task<IEnumerable<SubscriptionPlan>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(p => p.IsActive)
                .OrderBy(p => p.MonthlyPriceBRL)
                .ToListAsync(cancellationToken);
        }
    }
}
