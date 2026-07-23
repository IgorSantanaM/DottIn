using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Core.Models;

namespace DottIn.Domain.Subscriptions;

public enum WebhookProcessingStatus
{
    Processing = 0,
    Processed = 1,
    Failed = 2
}

public sealed class StripeWebhookReceipt : Entity<Guid>, IAggregateRoot
{
    public string EventId { get; private set; }
    public string EventType { get; private set; }
    public WebhookProcessingStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime ReceivedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? LastError { get; private set; }

    private StripeWebhookReceipt()
    {
        EventId = string.Empty;
        EventType = string.Empty;
    }

    public StripeWebhookReceipt(string eventId, string eventType)
    {
        if (string.IsNullOrWhiteSpace(eventId))
            throw new DomainException("O identificador do evento Stripe é obrigatório.");
        if (string.IsNullOrWhiteSpace(eventType))
            throw new DomainException("O tipo do evento Stripe é obrigatório.");

        Id = Guid.NewGuid();
        EventId = eventId;
        EventType = eventType;
        Status = WebhookProcessingStatus.Processing;
        AttemptCount = 1;
        ReceivedAt = DateTime.UtcNow;
        UpdatedAt = ReceivedAt;
    }

    public bool CanRetry(DateTime utcNow)
        => Status == WebhookProcessingStatus.Failed ||
           Status == WebhookProcessingStatus.Processing && UpdatedAt <= utcNow.AddMinutes(-5);

    public void BeginRetry()
    {
        if (Status == WebhookProcessingStatus.Processed)
            throw new DomainException("Um evento processado não pode ser repetido.");

        Status = WebhookProcessingStatus.Processing;
        AttemptCount++;
        LastError = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkProcessed()
    {
        Status = WebhookProcessingStatus.Processed;
        ProcessedAt = DateTime.UtcNow;
        UpdatedAt = ProcessedAt.Value;
        LastError = null;
    }

    public void MarkFailed(string error)
    {
        Status = WebhookProcessingStatus.Failed;
        LastError = string.IsNullOrWhiteSpace(error) ? "Erro não especificado." : error[..Math.Min(error.Length, 1000)];
        UpdatedAt = DateTime.UtcNow;
    }
}
