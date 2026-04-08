namespace DottIn.Application.Interfaces
{
    public interface IStripeService
    {
        Task<string> CreateCustomerAsync(string email, string name, Guid headquartersId, CancellationToken cancellationToken = default);
        
        Task<string> CreateCheckoutSessionAsync(
            string customerId, 
            string priceId, 
            Guid headquartersId,
            CancellationToken cancellationToken = default);
        
        Task<string> CreateCustomerPortalSessionAsync(
            string customerId, 
            CancellationToken cancellationToken = default);
        
        Task CancelSubscriptionAsync(
            string subscriptionId, 
            bool cancelImmediately = false,
            CancellationToken cancellationToken = default);

        Task<StripeSubscriptionInfo?> GetSubscriptionAsync(
            string subscriptionId,
            CancellationToken cancellationToken = default);

        StripeWebhookEvent? ParseWebhookEvent(string json, string signature);
    }

    public record StripeSubscriptionInfo(
        string Id,
        string Status,
        string CustomerId,
        string PriceId,
        DateTime CurrentPeriodStart,
        DateTime CurrentPeriodEnd,
        DateTime? CanceledAt);

    public record StripeWebhookEvent(
        string Type,
        string Json,
        object Data);
}
