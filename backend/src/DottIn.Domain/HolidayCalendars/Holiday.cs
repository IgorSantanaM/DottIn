using DottIn.Domain.Core.Models;

namespace DottIn.Domain.HolidayCalendars
{
    public class Holiday : ValueObject
    {
        public DateOnly Date { get; private set; }
        public string Name { get; private set; }
        public HolidayType Type { get; private set; }
        public bool IsOptional { get; private set; }

        private Holiday() { }

        public Holiday(DateOnly date, string name, HolidayType type, bool isOptional = false)
        {
            Date = date;
            Name = name;
            Type = type;
            IsOptional = isOptional;
        }

        public bool IsMandatory => !IsOptional;

        public bool IsWeekend => Date.DayOfWeek == DayOfWeek.Saturday || 
                                  Date.DayOfWeek == DayOfWeek.Sunday;

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Date;
            yield return Name;
            yield return Type;
            yield return IsOptional;
        }
    }
}
