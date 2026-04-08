namespace DottIn.Domain.Subscriptions
{
    public enum SubscriptionStatus
    {
        Free = 0,
        Active = 1,
        Trialing = 2,
        PastDue = 3,
        Canceled = 4,
        Incomplete = 5
    }
}
