using DottIn.Domain.Branches;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Core.Models;

namespace DottIn.Domain.TimeKeepings
{
    public class TimeKeeping : Entity<Guid>, IAggregateRoot
    {
        public Guid EmployeeId { get; private set; }
        public Guid BranchId { get; private set; }
        public TimeKeepingStatus Status => GetCurrentStatus();
        public DateOnly WorkDate { get; private set; }
        public string TimeZoneId { get; private set; } = "UTC";
        public DateTime CreatedAt { get; private set; }
        public Geolocation? Location { get; private set; }
        public ClockSource Source { get; private set; }
        public Guid ConcurrencyToken { get; private set; } = Guid.NewGuid();
        private readonly List<TimeEntry> _entries = new();
        public IReadOnlyCollection<TimeEntry> Entries => _entries.AsReadOnly();

        private TimeKeeping() { }

        public TimeKeeping(
            Guid branchId,
            Guid employeeId,
            Geolocation geolocation,
            DateOnly workDate,
            string timeZoneId,
            DateTime createdAtUtc,
            ClockSource source = ClockSource.Mobile)
        {
            if (branchId == Guid.Empty)
                throw new DomainException("Empresa inválida.");
            if (employeeId == Guid.Empty)
                throw new DomainException("Funcionário inválido.");
            if (geolocation is null)
                throw new DomainException("Geolocalização inválida.");

            BranchTime.Resolve(timeZoneId);
            BranchTime.NormalizeUtc(createdAtUtc);

            Id = Guid.NewGuid();
            BranchId = branchId;
            EmployeeId = employeeId;
            Location = geolocation;
            Source = source;
            CreatedAt = createdAtUtc;
            WorkDate = workDate;
            TimeZoneId = timeZoneId;
        }

        public void ClockIn(
            DateTime timeUtc,
            Geolocation? location = null,
            double? accuracyMeters = null,
            DateTime? capturedAtUtc = null,
            ClockSource source = ClockSource.Mobile)
        {
            if (_entries.Any())
                throw new DomainException("A jornada já foi iniciada.");

            AddEntry(timeUtc, TimeKeepingType.ClockIn, location, accuracyMeters, capturedAtUtc, source);
        }

        public void StartBreak(
            DateTime timeUtc,
            Geolocation? location = null,
            double? accuracyMeters = null,
            DateTime? capturedAtUtc = null,
            ClockSource source = ClockSource.Mobile)
        {
            if (Status != TimeKeepingStatus.Working)
                throw new DomainException("Só é possível iniciar um intervalo durante a jornada.");

            AddEntry(timeUtc, TimeKeepingType.BreakStart, location, accuracyMeters, capturedAtUtc, source);
        }

        public void EndBreak(
            DateTime timeUtc,
            Geolocation? location = null,
            double? accuracyMeters = null,
            DateTime? capturedAtUtc = null,
            ClockSource source = ClockSource.Mobile)
        {
            if (Status != TimeKeepingStatus.OnBreak)
                throw new DomainException("Não há intervalo em andamento para finalizar.");

            AddEntry(timeUtc, TimeKeepingType.BreakEnd, location, accuracyMeters, capturedAtUtc, source);
        }

        public void ClockOut(
            DateTime timeUtc,
            Geolocation? location = null,
            double? accuracyMeters = null,
            DateTime? capturedAtUtc = null,
            ClockSource source = ClockSource.Mobile)
        {
            if (Status == TimeKeepingStatus.NotStarted)
                throw new DomainException("Registre a entrada antes da saída.");
            if (Status == TimeKeepingStatus.Finished)
                throw new DomainException("A jornada já foi finalizada.");

            if (Status == TimeKeepingStatus.OnBreak)
                EndBreak(timeUtc, location, accuracyMeters, capturedAtUtc, source);

            AddEntry(timeUtc, TimeKeepingType.ClockOut, location, accuracyMeters, capturedAtUtc, source);
        }

        private void AddEntry(
            DateTime timeUtc,
            TimeKeepingType type,
            Geolocation? location,
            double? accuracyMeters,
            DateTime? capturedAtUtc,
            ClockSource source)
        {
            BranchTime.NormalizeUtc(timeUtc);
            if (timeUtc < CreatedAt)
                throw new DomainException("O registro não pode ser anterior ao início da jornada.");
            if (_entries.Any() && timeUtc < _entries.Last().Timestamp)
                throw new DomainException("O registro não pode ser anterior ao último evento da jornada.");

            _entries.Add(new TimeEntry(timeUtc, type, location, accuracyMeters, capturedAtUtc, source));
            ConcurrencyToken = Guid.NewGuid();
        }

        private TimeKeepingStatus GetCurrentStatus()
        {
            if (!_entries.Any()) return TimeKeepingStatus.NotStarted;

            return _entries.Last().Type switch
            {
                TimeKeepingType.ClockIn => TimeKeepingStatus.Working,
                TimeKeepingType.BreakStart => TimeKeepingStatus.OnBreak,
                TimeKeepingType.BreakEnd => TimeKeepingStatus.Working,
                TimeKeepingType.ClockOut => TimeKeepingStatus.Finished,
                _ => TimeKeepingStatus.NotStarted
            };
        }
    }
}
