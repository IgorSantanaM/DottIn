using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Core.Models;

namespace DottIn.Domain.Subscriptions
{
    public class TenantSubscription : Entity<Guid>, IAggregateRoot
    {
        public Guid HeadquartersId { get; private set; }
        public Guid OwnerId { get; private set; }
        public string StripeCustomerId { get; private set; }
        public string? StripeSubscriptionId { get; private set; }
        public Guid SubscriptionPlanId { get; private set; }
        public SubscriptionStatus Status { get; private set; }
        public DateTime CurrentPeriodStart { get; private set; }
        public DateTime CurrentPeriodEnd { get; private set; }
        public DateTime? CanceledAt { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        public SubscriptionPlan? Plan { get; private set; }

        private TenantSubscription() { }

        public TenantSubscription(
            Guid headquartersId,
            Guid ownerId,
            string stripeCustomerId,
            Guid subscriptionPlanId)
        {
            if (headquartersId == Guid.Empty)
                throw new DomainException("HeadquartersId é obrigatório.");
            if (ownerId == Guid.Empty)
                throw new DomainException("OwnerId é obrigatório.");
            if (string.IsNullOrWhiteSpace(stripeCustomerId))
                throw new DomainException("StripeCustomerId é obrigatório.");

            Id = Guid.NewGuid();
            HeadquartersId = headquartersId;
            OwnerId = ownerId;
            StripeCustomerId = stripeCustomerId;
            SubscriptionPlanId = subscriptionPlanId;
            Status = SubscriptionStatus.Free;
            CurrentPeriodStart = DateTime.UtcNow;
            CurrentPeriodEnd = DateTime.MaxValue;
            CreatedAt = DateTime.UtcNow;
        }

        public void Activate(string stripeSubscriptionId, Guid planId, DateTime periodStart, DateTime periodEnd)
        {
            if (string.IsNullOrWhiteSpace(stripeSubscriptionId))
                throw new DomainException("StripeSubscriptionId é obrigatório para ativar.");

            StripeSubscriptionId = stripeSubscriptionId;
            SubscriptionPlanId = planId;
            Status = SubscriptionStatus.Active;
            CurrentPeriodStart = periodStart;
            CurrentPeriodEnd = periodEnd;
            CanceledAt = null;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdatePlan(Guid newPlanId, DateTime periodStart, DateTime periodEnd)
        {
            SubscriptionPlanId = newPlanId;
            CurrentPeriodStart = periodStart;
            CurrentPeriodEnd = periodEnd;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdatePeriod(DateTime periodStart, DateTime periodEnd)
        {
            CurrentPeriodStart = periodStart;
            CurrentPeriodEnd = periodEnd;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkPastDue()
        {
            if (Status == SubscriptionStatus.PastDue) return;
            Status = SubscriptionStatus.PastDue;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkActive()
        {
            if (Status == SubscriptionStatus.Active) return;
            Status = SubscriptionStatus.Active;
            CanceledAt = null;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Cancel()
        {
            if (Status == SubscriptionStatus.Canceled) return;
            Status = SubscriptionStatus.Canceled;
            CanceledAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void RevertToFree(Guid freePlanId)
        {
            SubscriptionPlanId = freePlanId;
            StripeSubscriptionId = null;
            Status = SubscriptionStatus.Free;
            CurrentPeriodStart = DateTime.UtcNow;
            CurrentPeriodEnd = DateTime.MaxValue;
            CanceledAt = null;
            UpdatedAt = DateTime.UtcNow;
        }

        public void StartTrial(string stripeSubscriptionId, Guid planId, DateTime trialEnd)
        {
            StripeSubscriptionId = stripeSubscriptionId;
            SubscriptionPlanId = planId;
            Status = SubscriptionStatus.Trialing;
            CurrentPeriodStart = DateTime.UtcNow;
            CurrentPeriodEnd = trialEnd;
            UpdatedAt = DateTime.UtcNow;
        }

        public bool IsActiveOrTrialing => Status == SubscriptionStatus.Active || 
                                          Status == SubscriptionStatus.Trialing || 
                                          Status == SubscriptionStatus.Free;

        public bool IsPaid => Status == SubscriptionStatus.Active || Status == SubscriptionStatus.Trialing;
    }
}
