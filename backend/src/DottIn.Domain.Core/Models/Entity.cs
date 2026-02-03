namespace DottIn.Domain.Core.Models
{
    public abstract class Entity<TId> where TId : notnull
    {
        public TId? Id { get; protected set; }
        private readonly List<Event<TId>> _domainEvents = new();
        public IReadOnlyCollection<Event<TId>> DomainEvents => _domainEvents.AsReadOnly();

        public override bool Equals(object? obj)
        {
            var compareTo = obj as Entity<TId>;

            if (ReferenceEquals(this, compareTo)) return true;
            if (ReferenceEquals(null, compareTo)) return true;

            return Id!.Equals(compareTo.Id);
        }
        public void ClearDomainEvents()
            => _domainEvents.Clear();   

        public static bool operator ==(Entity<TId> entityA, Entity<TId> entityB)
        {
            if (ReferenceEquals(entityA, null) && ReferenceEquals(entityB, null))
                return true;

            if (ReferenceEquals(entityA, null) || ReferenceEquals(entityB, null))
                return false;

            return entityA.Equals(entityB);
        }

        public static bool operator !=(Entity<TId> entityA, Entity<TId> entityB)
            => !(entityA == entityB);

        public override int GetHashCode()
            => (GetType().GetHashCode() * 907) + Id!.GetHashCode();

        public override string ToString()
            => $"{GetType().Name} [Id={Id}]";

        protected void AddDomainEvent(Event<TId> domainEvent)
            => _domainEvents.Add(domainEvent);
    }
}
