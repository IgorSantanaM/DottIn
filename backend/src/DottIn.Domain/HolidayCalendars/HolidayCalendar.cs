using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Core.Models;

namespace DottIn.Domain.HolidayCalendars
{
    public class HolidayCalendar : Entity<Guid>, IAggregateRoot
    {
        public Guid BranchId { get; private set; }
        public string Name { get; private set; }
        public string? Description { get; private set; }
        public string CountryCode { get; private set; }
        public string? RegionCode { get; private set; }
        public int Year { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        private readonly List<Holiday> _holidays = [];
        public IReadOnlyCollection<Holiday> Holidays => _holidays.AsReadOnly();

        private HolidayCalendar() { }

        public HolidayCalendar(
            Guid branchId,
            string name,
            string countryCode,
            int year,
            string? regionCode = null,
            string? description = null)
        {
            if (branchId == Guid.Empty)
                throw new DomainException("O ID da empresa é obrigatório.");

            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("O nome do calendário é obrigatório.");

            if (string.IsNullOrWhiteSpace(countryCode))
                throw new DomainException("O código do país é obrigatório.");

            if (countryCode.Length != 2)
                throw new DomainException("O código do país deve ter 2 caracteres (ISO 3166-1 alpha-2).");

            if (regionCode is not null && regionCode.Length > 10)
                throw new DomainException("O código da região deve ter no máximo 10 caracteres.");

            var currentYear = DateTime.UtcNow.Year;
            if (year < currentYear - 1 || year > currentYear + 5)
                throw new DomainException($"O ano deve estar entre {currentYear - 1} e {currentYear + 5}.");

            Id = Guid.NewGuid();
            BranchId = branchId;
            Name = name.Trim();
            CountryCode = countryCode.ToUpperInvariant();
            RegionCode = regionCode?.ToUpperInvariant();
            Year = year;
            Description = description?.Trim();
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
        }

        public void AddHoliday(
            DateOnly date,
            string name,
            HolidayType type = HolidayType.National,
            bool isOptional = false)
        {
            ValidateHolidayDate(date);
            ValidateHolidayName(name);
            ValidateNoDuplicateHoliday(date);

            var holiday = new Holiday(date, name.Trim(), type, isOptional);
            _holidays.Add(holiday);
            UpdatedAt = DateTime.UtcNow;
        }

        public void AddHolidays(IEnumerable<(DateOnly Date, string Name, HolidayType Type, bool IsOptional)> holidays)
        {
            foreach (var (date, name, type, isOptional) in holidays)
            {
                AddHoliday(date, name, type, isOptional);
            }
        }

        public void RemoveHoliday(DateOnly date)
        {
            var holiday = _holidays.FirstOrDefault(h => h.Date == date)
                ?? throw new DomainException($"Feriado não encontrado para {date:dd/MM/yyyy}.");

            _holidays.Remove(holiday);
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateHoliday(
            DateOnly date,
            string? newName = null,
            HolidayType? newType = null,
            bool? isOptional = null)
        {
            var holiday = _holidays.FirstOrDefault(h => h.Date == date)
                ?? throw new DomainException($"Feriado não encontrado para {date:dd/MM/yyyy}.");

            var index = _holidays.IndexOf(holiday);

            var updatedHoliday = new Holiday(
                date,
                newName?.Trim() ?? holiday.Name,
                newType ?? holiday.Type,
                isOptional ?? holiday.IsOptional);

            _holidays[index] = updatedHoliday;
            UpdatedAt = DateTime.UtcNow;
        }

        public void ClearHolidays()
        {
            _holidays.Clear();
            UpdatedAt = DateTime.UtcNow;
        }


        public bool IsHoliday(DateOnly date) => _holidays.Any(h => h.Date == date);

        public bool IsHoliday(DateTime dateTime) => IsHoliday(DateOnly.FromDateTime(dateTime));

        public bool IsMandatoryHoliday(DateOnly date) =>
            _holidays.Any(h => h.Date == date && !h.IsOptional);

        public bool IsOptionalHoliday(DateOnly date) =>
            _holidays.Any(h => h.Date == date && h.IsOptional);

        public Holiday? GetHoliday(DateOnly date) =>
            _holidays.FirstOrDefault(h => h.Date == date);

        public IEnumerable<Holiday> GetHolidaysByType(HolidayType type) =>
            _holidays.Where(h => h.Type == type).OrderBy(h => h.Date);

        public IEnumerable<Holiday> GetHolidaysInRange(DateOnly startDate, DateOnly endDate) =>
            _holidays.Where(h => h.Date >= startDate && h.Date <= endDate).OrderBy(h => h.Date);

        public IEnumerable<Holiday> GetMandatoryHolidays() =>
            _holidays.Where(h => !h.IsOptional).OrderBy(h => h.Date);

        public IEnumerable<Holiday> GetOptionalHolidays() =>
            _holidays.Where(h => h.IsOptional).OrderBy(h => h.Date);

        public int CountWorkingDays(DateOnly startDate, DateOnly endDate)
        {
            if (startDate > endDate)
                throw new DomainException("A data inicial não pode ser maior que a data final.");

            var workingDays = 0;
            var currentDate = startDate;

            while (currentDate <= endDate)
            {
                var dayOfWeek = currentDate.DayOfWeek;
                var isWeekend = dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday;
                var isMandatoryHoliday = IsMandatoryHoliday(currentDate);

                if (!isWeekend && !isMandatoryHoliday)
                    workingDays++;

                currentDate = currentDate.AddDays(1);
            }

            return workingDays;
        }

        public void Update(string name, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("O nome do calendário é obrigatório.");

            Name = name.Trim();
            Description = description?.Trim();
            UpdatedAt = DateTime.UtcNow;
        }

        public void Activate()
        {
            if (IsActive) return;
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            if (!IsActive) return;
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public HolidayCalendar CloneForYear(int newYear)
        {
            var currentYear = DateTime.UtcNow.Year;
            if (newYear < currentYear || newYear > currentYear + 5)
                throw new DomainException($"O ano deve estar entre {currentYear} e {currentYear + 5}.");

            var newCalendar = new HolidayCalendar(
                BranchId,
                $"{Name} ({newYear})",
                CountryCode,
                newYear,
                RegionCode,
                Description);

            foreach (var holiday in _holidays)
            {
                var newDate = new DateOnly(newYear, holiday.Date.Month, holiday.Date.Day);
                newCalendar.AddHoliday(newDate, holiday.Name, holiday.Type, holiday.IsOptional);
            }

            return newCalendar;
        }

        public HolidayCalendar CloneForBranch(Guid newBranchId)
        {
            if (newBranchId == Guid.Empty)
                throw new DomainException("O ID da nova filial é obrigatório.");

            var newCalendar = new HolidayCalendar(
                newBranchId,
                Name,
                CountryCode,
                Year,
                RegionCode,
                Description);

            foreach (var holiday in _holidays)
            {
                newCalendar.AddHoliday(holiday.Date, holiday.Name, holiday.Type, holiday.IsOptional);
            }

            return newCalendar;
        }

        private void ValidateHolidayDate(DateOnly date)
        {
            if (date.Year != Year)
                throw new DomainException($"A data do feriado deve ser do ano {Year}.");
        }

        private static void ValidateHolidayName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("O nome do feriado é obrigatório.");

            if (name.Length > 100)
                throw new DomainException("O nome do feriado deve ter no máximo 100 caracteres.");
        }

        private void ValidateNoDuplicateHoliday(DateOnly date)
        {
            if (_holidays.Any(h => h.Date == date))
                throw new DomainException($"Já existe um feriado cadastrado para {date:dd/MM/yyyy}.");
        }
    }
}
