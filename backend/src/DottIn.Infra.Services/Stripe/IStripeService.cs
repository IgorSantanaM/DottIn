using Stripe;

namespace DottIn.Infra.Services.Stripe
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

        Task<Subscription?> GetSubscriptionAsync(
            string subscriptionId,
            CancellationToken cancellationToken = default);

        Event? ParseWebhookEvent(string json, string signature);
    }
}
