using DottIn.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Stripe;
using Stripe.Checkout;
using BillingPortal = Stripe.BillingPortal;
using AppIStripeService = DottIn.Application.Interfaces.IStripeService;

namespace DottIn.Infra.Services.Stripe
{
    public class StripeService : IStripeService, AppIStripeService
    {
        private readonly StripeSettings _settings;
        private readonly CustomerService _customerService;
        private readonly SessionService _checkoutSessionService;
        private readonly BillingPortal.SessionService _portalSessionService;
        private readonly SubscriptionService _subscriptionService;
        private readonly ILogger<StripeService> _logger;
        private readonly ResiliencePipeline _retryPipeline;

        private static readonly HashSet<string> RetryableErrorCodes = new()
        {
            "rate_limit", "lock_timeout", "api_connection_error"
        };

        public StripeService(IOptions<StripeSettings> settings, ILogger<StripeService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
            StripeConfiguration.ApiKey = _settings.SecretKey;

            _customerService = new CustomerService();
            _checkoutSessionService = new SessionService();
            _portalSessionService = new BillingPortal.SessionService();
            _subscriptionService = new SubscriptionService();

            _retryPipeline = new ResiliencePipelineBuilder()
                .AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<StripeException>(IsRetryableError),
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(2),
                    BackoffType = DelayBackoffType.Exponential,
                    OnRetry = args =>
                    {
                        _logger.LogWarning(
                            "Stripe API call failed (attempt {RetryCount}), retrying in {RetryDelay}s. Error: {Error}",
                            args.AttemptNumber + 1, args.RetryDelay.TotalSeconds, args.Outcome.Exception?.Message);
                        return ValueTask.CompletedTask;
                    }
                })
                .Build();
        }

        private static bool IsRetryableError(StripeException ex)
        {
            if (ex.StripeError == null) return true;
            
            return RetryableErrorCodes.Contains(ex.StripeError.Code ?? string.Empty) ||
                   ex.HttpStatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                   ex.HttpStatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                   ex.HttpStatusCode == System.Net.HttpStatusCode.GatewayTimeout;
        }

        public async Task<string> CreateCustomerAsync(
            string email, 
            string name, 
            Guid headquartersId,
            CancellationToken cancellationToken = default)
        {
            return await _retryPipeline.ExecuteAsync(async ct =>
            {
                var options = new CustomerCreateOptions
                {
                    Email = email,
                    Name = name,
                    Metadata = new Dictionary<string, string>
                    {
                        { "headquarters_id", headquartersId.ToString() }
                    }
                };

                var customer = await _customerService.CreateAsync(options, cancellationToken: ct);
                _logger.LogInformation("Created Stripe customer {CustomerId} for HQ {HeadquartersId}", customer.Id, headquartersId);
                return customer.Id;
            }, cancellationToken);
        }

        public async Task<string> CreateCheckoutSessionAsync(
            string customerId, 
            string priceId,
            Guid headquartersId,
            CancellationToken cancellationToken = default)
        {
            return await _retryPipeline.ExecuteAsync(async ct =>
            {
                var idempotencyKey = GenerateIdempotencyKey("checkout", customerId, priceId, headquartersId);
                
                var options = new SessionCreateOptions
                {
                    Customer = customerId,
                    Mode = "subscription",
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            Price = priceId,
                            Quantity = 1
                        }
                    },
                    SuccessUrl = $"{_settings.SuccessUrl}?session_id={{CHECKOUT_SESSION_ID}}",
                    CancelUrl = _settings.CancelUrl,
                    Metadata = new Dictionary<string, string>
                    {
                        { "headquarters_id", headquartersId.ToString() }
                    },
                    SubscriptionData = new SessionSubscriptionDataOptions
                    {
                        Metadata = new Dictionary<string, string>
                        {
                            { "headquarters_id", headquartersId.ToString() }
                        }
                    },
                    PaymentMethodTypes = new List<string> { "card" },
                    BillingAddressCollection = "required",
                    AllowPromotionCodes = true
                };

                var requestOptions = new RequestOptions
                {
                    IdempotencyKey = idempotencyKey
                };

                var session = await _checkoutSessionService.CreateAsync(options, requestOptions, ct);
                _logger.LogInformation("Created checkout session {SessionId} for customer {CustomerId}", session.Id, customerId);
                return session.Url;
            }, cancellationToken);
        }

        public async Task<string> CreateCustomerPortalSessionAsync(
            string customerId,
            CancellationToken cancellationToken = default)
        {
            return await _retryPipeline.ExecuteAsync(async ct =>
            {
                var options = new BillingPortal.SessionCreateOptions
                {
                    Customer = customerId,
                    ReturnUrl = _settings.PortalReturnUrl
                };

                var session = await _portalSessionService.CreateAsync(options, cancellationToken: ct);
                return session.Url;
            }, cancellationToken);
        }

        public async Task CancelSubscriptionAsync(
            string subscriptionId, 
            bool cancelImmediately = false,
            CancellationToken cancellationToken = default)
        {
            await _retryPipeline.ExecuteAsync(async ct =>
            {
                if (cancelImmediately)
                {
                    await _subscriptionService.CancelAsync(subscriptionId, cancellationToken: ct);
                    _logger.LogInformation("Immediately cancelled subscription {SubscriptionId}", subscriptionId);
                }
                else
                {
                    var options = new SubscriptionUpdateOptions
                    {
                        CancelAtPeriodEnd = true
                    };
                    await _subscriptionService.UpdateAsync(subscriptionId, options, cancellationToken: ct);
                    _logger.LogInformation("Scheduled cancellation at period end for subscription {SubscriptionId}", subscriptionId);
                }
                return true;
            }, cancellationToken);
        }

        public async Task<Subscription?> GetSubscriptionAsync(
            string subscriptionId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _retryPipeline.ExecuteAsync(async ct =>
                    await _subscriptionService.GetAsync(subscriptionId, cancellationToken: ct), cancellationToken);
            }
            catch (StripeException ex) when (ex.StripeError?.Code == "resource_missing")
            {
                return null;
            }
        }

        async Task<StripeSubscriptionInfo?> AppIStripeService.GetSubscriptionAsync(
            string subscriptionId,
            CancellationToken cancellationToken)
        {
            var subscription = await GetSubscriptionAsync(subscriptionId, cancellationToken);
            if (subscription == null)
                return null;

            var firstItem = subscription.Items.Data.FirstOrDefault();
            return new StripeSubscriptionInfo(
                Id: subscription.Id,
                Status: subscription.Status,
                CustomerId: subscription.CustomerId,
                PriceId: firstItem?.Price?.Id ?? string.Empty,
                CurrentPeriodStart: firstItem?.CurrentPeriodStart ?? DateTime.MinValue,
                CurrentPeriodEnd: firstItem?.CurrentPeriodEnd ?? DateTime.MinValue,
                CanceledAt: subscription.CanceledAt);
        }

        public Event? ParseWebhookEvent(string json, string signature)
        {
            try
            {
                return EventUtility.ConstructEvent(
                    json,
                    signature,
                    _settings.WebhookSecret,
                    throwOnApiVersionMismatch: false);
            }
            catch (StripeException ex)
            {
                _logger.LogWarning("Failed to parse webhook event: {Error}", ex.Message);
                return null;
            }
        }

        StripeWebhookEvent? AppIStripeService.ParseWebhookEvent(string json, string signature)
        {
            var stripeEvent = ParseWebhookEvent(json, signature);
            if (stripeEvent == null)
                return null;

            return new StripeWebhookEvent(
                Type: stripeEvent.Type,
                Json: json,
                Data: stripeEvent.Data.Object);
        }

        private static string GenerateIdempotencyKey(string operation, string customerId, string priceId, Guid headquartersId)
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd-HH");
            return $"{operation}_{customerId}_{priceId}_{headquartersId}_{today}";
        }
    }
}
