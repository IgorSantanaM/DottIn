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
        public DateTime CreatedAt { get; private set; }
        public Geolocation? Location { get; private set; }
        private readonly List<TimeEntry> _entries = new();
        public IReadOnlyCollection<TimeEntry> Entries => _entries.AsReadOnly();

        private TimeKeeping() { }

        public TimeKeeping(Guid branchId, Guid employeeId, Geolocation geolocation)
        {
            Id = Guid.NewGuid();

            if (branchId == Guid.Empty)
                throw new DomainException("Empresa Invalida.");

            if (employeeId == Guid.Empty)
                throw new DomainException("Funcionário Invalido.");

            BranchId = branchId;
            EmployeeId = employeeId;
            Location = geolocation;
            CreatedAt = DateTime.UtcNow;
            WorkDate = DateOnly.FromDateTime(CreatedAt);
        }

        public void ClockIn(DateTime time)
        {
            if (_entries.Any())
                throw new DomainException("Tempo de serviço ja foi iniciado.");

            AddEntry(time, TimeKeepingType.ClockIn);
        }

        public void StartBreak(DateTime time)
        {
            if (Status != TimeKeepingStatus.Working)
                throw new DomainException("Não é permitido começar um intervalo se não está em serviço.");

            AddEntry(time, TimeKeepingType.BreakStart);
        }

        public void EndBreak(DateTime time)
        {
            if (Status != TimeKeepingStatus.OnBreak)
                throw new DomainException("Não é permitido encessar um intervalo se não está em um intervalo.");

            AddEntry(time, TimeKeepingType.BreakEnd);
        }

        public void ClockOut(DateTime time)
        {
            if (Status == TimeKeepingStatus.Finished)
                throw new DomainException("Já Finalizado");

            if (Status == TimeKeepingStatus.OnBreak)
                EndBreak(time);

            AddEntry(time, TimeKeepingType.ClockOut);
        }

        private void AddEntry(DateTime time, TimeKeepingType type)
        {
            if (_entries.Any() && time < _entries.Last().Timestamp)
                throw new DomainException("Não é permitido adicionar uma entrada que é anterior que a última.");

            _entries.Add(new TimeEntry(time, type));
        }

        private TimeKeepingStatus GetCurrentStatus()
        {
            if (!_entries.Any()) return TimeKeepingStatus.NotStarted;

            var lastType = _entries.Last().Type;
            return lastType switch
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
