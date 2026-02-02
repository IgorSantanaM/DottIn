namespace DottIn.Domain.Core.Models
{
    public abstract record Message<TId> where TId : notnull
    {
        public string MessageType { get; protected set; }
        public TId? AggregateId { get; protected set; }

        protected Message(TId aggregateId)
        {
            AggregateId = aggregateId;
            MessageType = GetType().Name;
        }
    }
}
