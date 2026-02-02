using MediatR;

namespace DottIn.Domain.Core.Models
{
    public abstract record Event<TId> : Message<TId>, INotification where TId : notnull
    {
        public DateTime TimeStamp { get; init; }

        protected Event(TId aggregateId) : base(aggregateId)
        {
            TimeStamp = DateTime.Now;
        }
    }
}
