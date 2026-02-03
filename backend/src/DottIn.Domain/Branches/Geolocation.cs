using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Core.Models;

namespace DottIn.Domain.ValueObjects
{
    public class Geolocation : ValueObject
    {
        #region Geolocation variables
        private const double MinLatitude = -90.0;
        private const double MaxLatitude = 90.0;
        private const double MinLongitude = -180.0;
        private const double MaxLongitude = 180.0;
        #endregion

        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        private Geolocation() { }

        public Geolocation(double latitude, double longitude)
        {
            if (latitude < MinLatitude || latitude > MaxLatitude)
                throw new DomainException($"Latitude inválida: {latitude}. Deve estar entre -90 e 90.");

            if (longitude < MinLongitude || longitude > MaxLongitude)
                throw new DomainException($"Longitude inválida: {longitude}. Deve estar entre -180 e 180.");

            Latitude = latitude;
            Longitude = longitude;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Latitude;
            yield return Longitude;
        }
    }
}