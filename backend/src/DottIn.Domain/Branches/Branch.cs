using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Core.Models;
using DottIn.Domain.ValueObjects;
using System;

namespace DottIn.Domain.Branches
{
    public class Branch : Entity<Guid>, IAggregateRoot
    {
        public string Name { get; private set; }
        public Document Document { get; private set; }
        public string? Email { get; private set; }
        public string? PhoneNumber { get; private set; }
        public Address Address { get; private set; }
        public Geolocation Geolocation { get; private set; }
        public string TimeZoneId { get; private set; } 
        public int AllowedRadiusMeters { get; private set; }
        public int ToleranceMinutes { get; set; }
        public Guid? HolidayCalendarId { get; private set; }
        public bool AllowOvernightShifts { get; private set; } 
        public bool IsActive { get; private set; }
        public bool IsHeadquarters { get; private set; }
        public string? OwnerId { get; private set; }
        public TimeOnly StartWorkTime { get; private set; }
        public TimeOnly EndWorkTime { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        private Branch() { }

        public Branch(
            string name,
            Document document,
            Geolocation geolocation,
            Address address,
            string timeZoneId,
            TimeOnly startWorkTime,
            TimeOnly endWorkTime,
            string? email = null,
            string? phoneNumber = null,
            string? ownerId = null,
            bool isHeadquarters = false,
            int allowedRadiusMeters = 100,
            int toleranceMinutes = 10) 
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("O Nome da filial é obrigatório.");

            if (document.Type != DocumentType.CNPJ)
                throw new DomainException("Uma filial deve ser registrada com um CNPJ.");

            if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phoneNumber))
                throw new DomainException("Informe ao menos um contato (E-mail ou Telefone).");

            try 
            {
                TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch
            {
                throw new DomainException($"Fuso horário inválido: {timeZoneId}");
            }

            if (allowedRadiusMeters < 10)
                throw new DomainException("O raio de permissão deve ser de no mínimo 10 metros.");
            if (toleranceMinutes < 5)
                throw new DomainException("A tolerância em minutos não deve ser menor do que 5 minutos.");

            Name = name;
            Document = document;
            Geolocation = geolocation;
            Address = address;
            TimeZoneId = timeZoneId;
            AllowedRadiusMeters = allowedRadiusMeters;
            StartWorkTime = startWorkTime;
            EndWorkTime = endWorkTime;
            Email = email;
            PhoneNumber = phoneNumber;
            OwnerId = ownerId;
            IsHeadquarters = isHeadquarters;
            ToleranceMinutes = toleranceMinutes;
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
        }

        public void MoveLocation(Address newAddress, Geolocation newLocation, string? newTimeZoneId = null)
        {
            Address = newAddress ?? throw new DomainException("Endereço inválido");
            Geolocation = newLocation ?? throw new DomainException("Geolocalização inválida");

            if (!string.IsNullOrEmpty(newTimeZoneId))
            {
                try { TimeZoneInfo.FindSystemTimeZoneById(newTimeZoneId); }
                catch { throw new DomainException($"Fuso horário inválido: {newTimeZoneId}"); }
                TimeZoneId = newTimeZoneId;
            }

            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateConfig(int allowedRadiusMeters, string timeZoneId)
        {
            if (allowedRadiusMeters < 10)
                throw new DomainException("O raio de permissão deve ser de no mínimo 10 metros.");

            try { TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
            catch { throw new DomainException($"Fuso horário inválido: {timeZoneId}"); }

            AllowedRadiusMeters = allowedRadiusMeters;
            TimeZoneId = timeZoneId;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateSchedule(TimeOnly start, TimeOnly end)
        {
            var duration = start < end
                        ? end - start
                        : (TimeSpan.FromHours(24) - start.ToTimeSpan()) + end.ToTimeSpan();

            if (duration.TotalHours < 1) 
                throw new DomainException("O turno de trabalho deve ter pelo menos 1 hora.");

            StartWorkTime = start;
            EndWorkTime = end;

            UpdatedAt = DateTime.UtcNow;
        }

        public void SetOwner(string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
                throw new DomainException("Por favor, informe qual será o dono.");

            OwnerId = ownerId;
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

        public void SetComplianceRules(int toleranceMinutes, Guid? holidayCalendarId)
        {
            if (toleranceMinutes < 0)
                throw new DomainException("Tolerância não pode ser negativa.");

            ToleranceMinutes = toleranceMinutes;
            HolidayCalendarId = holidayCalendarId;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}