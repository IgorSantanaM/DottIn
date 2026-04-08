using DottIn.Domain.Core.Models;

namespace DottIn.Domain.Subscriptions
{
    public class SubscriptionPlan : Entity<Guid>, IAggregateRoot
    {
        public string Name { get; private set; }
        public string? StripePriceId { get; private set; }
        public int MaxEmployees { get; private set; }
        public int MaxBranches { get; private set; }
        public decimal MonthlyPriceBRL { get; private set; }
        public string? FeaturesJson { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        private SubscriptionPlan() { }

        public SubscriptionPlan(
            string name,
            int maxEmployees,
            int maxBranches,
            decimal monthlyPriceBRL,
            string? stripePriceId = null,
            string? featuresJson = null)
        {
            Id = Guid.NewGuid();
            Name = name;
            MaxEmployees = maxEmployees;
            MaxBranches = maxBranches;
            MonthlyPriceBRL = monthlyPriceBRL;
            StripePriceId = stripePriceId;
            FeaturesJson = featuresJson;
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
        }

        public static SubscriptionPlan CreateFreePlan()
        {
            return new SubscriptionPlan(
                name: "Free",
                maxEmployees: 5,
                maxBranches: 1,
                monthlyPriceBRL: 0,
                stripePriceId: null,
                featuresJson: null);
        }

        public bool HasUnlimitedEmployees => MaxEmployees == -1;
        public bool HasUnlimitedBranches => MaxBranches == -1;
        public bool IsFree => MonthlyPriceBRL == 0;

        public bool CanAddEmployee(int currentCount)
        {
            if (HasUnlimitedEmployees) return true;
            return currentCount < MaxEmployees;
        }

        public bool CanAddBranch(int currentCount)
        {
            if (HasUnlimitedBranches) return true;
            return currentCount < MaxBranches;
        }

        public void UpdateStripePriceId(string priceId)
        {
            StripePriceId = priceId;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Activate()
        {
            if (IsActive) return;
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            if (!IsActive) return;
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
