using DottIn.Domain.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DottIn.Domain.TimeKeepings
{
    public class TimeEntry : ValueObject
    {
        public DateTime Timestamp { get; set; }
        public TimeKeepingType Type { get; set; }

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
