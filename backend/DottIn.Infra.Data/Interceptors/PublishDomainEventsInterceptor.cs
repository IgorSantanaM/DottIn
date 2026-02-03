using DottIn.Domain.Core.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DottIn.Infra.Data.Interceptors
{
    public class PublishDomainEventsInterceptor(IPublisher mediator) : SaveChangesInterceptor
    {

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            await CollectAndPublishDomainEvents(eventData.Context);

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }


        private async Task CollectAndPublishDomainEvents(DbContext? context)
        {
            if (context is null) return;

            var entitiesWithEvents = context.ChangeTracker
               .Entries<Entity<Guid>>()
               .Where(e => e.Entity.DomainEvents.Any())
               .Select(e => e.Entity)
               .ToList();

            if (entitiesWithEvents.Count == 0) return;

            var domainEvents = entitiesWithEvents
                .SelectMany(e => e.DomainEvents)
                .ToList();

            entitiesWithEvents.ForEach(e => e.ClearDomainEvents());

            foreach (var domainEvent in domainEvents)
                await mediator.Publish(domainEvent);
        }
    }
}
