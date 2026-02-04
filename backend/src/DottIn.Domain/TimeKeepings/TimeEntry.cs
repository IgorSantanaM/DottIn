using DottIn.Domain.Core.Models;

namespace DottIn.Domain.TimeKeepings
{
    public class TimeEntry : ValueObject // TODO: Have each GEOLOCATION PER ENTRY?
    {
        public DateTime Timestamp { get; private set; }
        public TimeKeepingType Type { get; private set; }

        public TimeEntry(DateTime timestamp, TimeKeepingType type)
        {
            Timestamp = timestamp;
            Type = type;
        }
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Timestamp;
            yield return Type;
        }
    }
}
