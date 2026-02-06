using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Core.Models;
using DottIn.Domain.ValueObjects;

namespace DottIn.Domain.Branches
{
    public class Branch : Entity<Guid>, IAggregateRoot
    {
        #region Geolocation variables
        private const double EarthRadiusMeters = 6371000;
        private const double MinLatitude = -90.0;
        private const double MaxLatitude = 90.0;
        private const double MinLongitude = -180.0;
        private const double MaxLongitude = 180.0;
        #endregion

        public string Name { get; private set; }
        public Document Document { get; private set; }
        public string? Email { get; private set; }
        public string? PhoneNumber { get; private set; }
        public Address Address { get; private set; }
        public Geolocation Location { get; private set; }
        public string TimeZoneId { get; private set; }
        public int AllowedRadiusMeters { get; private set; }
        public int ToleranceMinutes { get; private set; }
        public Guid? HolidayCalendarId { get; private set; }
        public bool AllowOvernightShifts { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsHeadquarters { get; private set; }
        public Guid OwnerId { get; private set; }
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
            Guid ownerId,
            string? email = null,
            string? phoneNumber = null,
            bool isHeadquarters = false,
            int allowedRadiusMeters = 100,
            int toleranceMinutes = 10)
        {

            Id = Guid.NewGuid();
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("O Nome da Empresa é obrigatório.");

            if (document.Type != DocumentType.CNPJ)
                throw new DomainException("Uma Empresa deve ser registrada com um CNPJ.");

            if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phoneNumber))
                throw new DomainException("Informe ao menos um contato (E-mail ou Telefone).");

            ValidateTimezone(timeZoneId);

            if (allowedRadiusMeters < 10)
                throw new DomainException("O raio de permissão deve ser de no mínimo 10 metros.");

            if (toleranceMinutes < 5)
                throw new DomainException("A tolerância em minutos não deve ser menor do que 5 minutos.");

            SetScheduleInternal(startWorkTime, endWorkTime);

            Name = name;
            Document = document;
            Location = geolocation;
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
            Location = newLocation ?? throw new DomainException("Geolocalização inválida");

            if (!string.IsNullOrEmpty(newTimeZoneId))
            {
                ValidateTimezone(newTimeZoneId);
                TimeZoneId = newTimeZoneId;
            }

            UpdatedAt = DateTime.UtcNow;
        }
        public bool IsWithinRange(double userLat, double userLon)
        {
            if (userLat < MinLatitude || userLat > MaxLatitude ||
                userLon < MinLongitude || userLon > MaxLongitude)
            {
                return false;
            }

            var dLat = ToRadians(userLat - Location.Latitude);
            var dLon = ToRadians(userLon - Location.Longitude);

            var lat1 = ToRadians(Location.Latitude);
            var lat2 = ToRadians(userLat);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            var distance = EarthRadiusMeters * c;

            return distance <= AllowedRadiusMeters;
        }


        public void UpdateConfig(int allowedRadiusMeters, string timeZoneId)
        {
            if (allowedRadiusMeters < 10)
                throw new DomainException("O raio de permissão deve ser de no mínimo 10 metros.");

            ValidateTimezone(timeZoneId);

            AllowedRadiusMeters = allowedRadiusMeters;
            TimeZoneId = timeZoneId;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateSchedule(TimeOnly start, TimeOnly end)
        {
            SetScheduleInternal(start, end);

            UpdatedAt = DateTime.UtcNow;
        }

        public void SetOwner(Guid ownerId)
        {
            if (Guid.Empty == ownerId)
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

        private void SetScheduleInternal(TimeOnly start, TimeOnly end)
        {
            var duration = start < end
                ? end - start
                : (TimeSpan.FromHours(24) - start.ToTimeSpan()) + end.ToTimeSpan();

            if (duration.TotalHours < 1)
                throw new DomainException("O turno de trabalho deve ter pelo menos 1 hora.");

            StartWorkTime = start;
            EndWorkTime = end;

            AllowOvernightShifts = start > end;
        }

        private static void ValidateTimezone(string newTimeZoneId)
        {
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(newTimeZoneId);
            }

            catch
            {
                throw new DomainException($"Fuso horário inválido: {newTimeZoneId}");
            }
        }
        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}