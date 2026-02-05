using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Core.Models;
using DottIn.Domain.ValueObjects;

namespace DottIn.Domain.Employees
{
    public class Employee : Entity<Guid>, IAggregateRoot
    {
        public string Name { get; private set; }
        public Document CPF { get; private set; }
        public string? ImageUrl { get; private set; }
        public Guid BranchId { get; private set; }
        public TimeOnly StartWorkTime { get; private set; }
        public TimeOnly EndWorkTime { get; private set; }
        public TimeOnly IntervalStart { get; private set; }
        public TimeOnly IntervalEnd { get; private set; }
        public DateTime CreatedAt { get; private init; }
        public DateTime? UpdatedAt { get; private set; }
        public bool IsActive { get; private set; }
        public bool AllowOvernightShifts { get; private set; }

        private Employee() { }

        public Employee(
            string name,
            Document cpf,
            Guid branchId,
            TimeOnly startWorkTime,
            TimeOnly endWorkTime,
            TimeOnly intervalStart,
            TimeOnly intervalEnd)
        {

            Id = Guid.NewGuid();
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("O nome não pode ser vazio.");

            if (cpf == null)
                throw new DomainException("CPF é obrigatório");

            if (cpf.Type != DocumentType.CPF)
                throw new DomainException("Funcionário deve ser registrado com um CPF.");

            if (branchId == Guid.Empty)
                throw new DomainException("O funcionário tem que ser vinculado à uma empresa.");

            SetScheduleInternal(startWorkTime, endWorkTime, intervalStart, intervalEnd);

            Name = name;
            CPF = cpf;
            BranchId = branchId;
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
        }

        public void AddImage(string imageUrl)
        {
            if(string.IsNullOrEmpty(imageUrl))
                throw new DomainException("A Imagem deve ser inserida.");

            ImageUrl = imageUrl;
        }

        public void UpdateProfile(string name, string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Nome inválido");
            Name = name;
            ImageUrl = imageUrl;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateSchedule(TimeOnly start, TimeOnly end, TimeOnly intervalStart, TimeOnly intervalEnd)
        {
            SetScheduleInternal(start, end, intervalStart, intervalEnd);
            UpdatedAt = DateTime.UtcNow;
        }

        public void Activate()
        {
            if (IsActive) return;
            IsActive = true;
            UpdatedAt = DateTime.Now;
        }

        public void Deactivate()
        {
            if (!IsActive) return;
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        private void SetScheduleInternal(TimeOnly start, TimeOnly end, TimeOnly intStart, TimeOnly intEnd)
        {
            var shiftDuration = CalculateDuration(start, end);
            if (shiftDuration.TotalHours < 1)
                throw new DomainException("O turno de trabalho deve ter pelo menos 1 hora.");

            var intervalDuration = CalculateDuration(intStart, intEnd);
            if (intervalDuration.TotalMinutes < 15)
                throw new DomainException("O intervalo deve ter no mínimo 15 minutos.");

            bool isOvernightShift = start > end;

            if (!IsIntervalWithinShift(start, end, intStart, intEnd))
            {
                throw new DomainException("O horário de intervalo deve estar dentro do horário de expediente.");
            }

            StartWorkTime = start;
            EndWorkTime = end;
            IntervalStart = intStart;
            IntervalEnd = intEnd;
            AllowOvernightShifts = isOvernightShift;
        }

        private TimeSpan CalculateDuration(TimeOnly start, TimeOnly end)
        {
            return start <= end
                ? end - start
                : (TimeSpan.FromHours(24) - start.ToTimeSpan()) + end.ToTimeSpan();
        }

        private bool IsIntervalWithinShift(TimeOnly start, TimeOnly end, TimeOnly intStart, TimeOnly intEnd)
        {
            bool isOvernight = start > end;
            bool isIntervalOvernight = intStart > intEnd;

            double startMin = 0;
            double endMin = (isOvernight ? (TimeSpan.FromHours(24) - start.ToTimeSpan()) + end.ToTimeSpan() : end - start).TotalMinutes;

            double intStartMin = GetOffsetMinutes(start, intStart);
            double intEndMin = GetOffsetMinutes(start, intEnd);

            if (intStartMin < 0) return false;
            if (intEndMin > endMin) return false;

            return true;
        }

        private double GetOffsetMinutes(TimeOnly referenceStart, TimeOnly target)
        {
            if (target >= referenceStart)
                return (target - referenceStart).TotalMinutes;
            else
                return (TimeSpan.FromHours(24) - referenceStart.ToTimeSpan() + target.ToTimeSpan()).TotalMinutes;
        }
    }
}
