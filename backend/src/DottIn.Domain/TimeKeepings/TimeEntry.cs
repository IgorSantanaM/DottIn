using DottIn.Domain.Core.Models;
using DottIn.Domain.Branches;

namespace DottIn.Domain.TimeKeepings
{
    public class TimeEntry : ValueObject
    {
        public DateTime Timestamp { get; private set; }
        public TimeKeepingType Type { get; private set; }
        public Geolocation? Location { get; private set; }
        public double? AccuracyMeters { get; private set; }
        public DateTime? CapturedAtUtc { get; private set; }
        public ClockSource Source { get; private set; }

        private TimeEntry() { }

        public TimeEntry(
            DateTime timestamp,
            TimeKeepingType type,
            Geolocation? location = null,
            double? accuracyMeters = null,
            DateTime? capturedAtUtc = null,
            ClockSource source = ClockSource.Mobile)
        {
            Timestamp = timestamp;
            Type = type;
            Location = location;
            AccuracyMeters = accuracyMeters;
            CapturedAtUtc = capturedAtUtc;
            Source = source;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Timestamp;
            yield return Type;
            if (Location is not null)
                yield return Location;
            if (AccuracyMeters.HasValue)
                yield return AccuracyMeters.Value;
            if (CapturedAtUtc.HasValue)
                yield return CapturedAtUtc.Value;
            yield return Source;
        }
    }
}
