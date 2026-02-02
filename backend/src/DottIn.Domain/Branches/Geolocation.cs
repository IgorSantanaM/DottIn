using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Core.Models;
using System;
using System.Collections.Generic;

namespace DottIn.Domain.ValueObjects
{
    public class Geolocation : ValueObject
    {
        private const double MinLatitude = -90.0;
        private const double MaxLatitude = 90.0;
        private const double MinLongitude = -180.0;
        private const double MaxLongitude = 180.0;
        private const double EarthRadiusMeters = 6371000;

        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public int AllowedRadiusMeters { get; private set; }

        private Geolocation() { }

        public Geolocation(double latitude, double longitude, int allowedRadiusMeters)
        {
            if (latitude < MinLatitude || latitude > MaxLatitude)
                throw new DomainException($"Latitude inválida: {latitude}. Deve estar entre -90 e 90.");

            if (longitude < MinLongitude || longitude > MaxLongitude)
                throw new DomainException($"Longitude inválida: {longitude}. Deve estar entre -180 e 180.");

            if (allowedRadiusMeters <= 0)
                throw new DomainException("O raio permitido deve ser maior que zero metros.");

            Latitude = latitude;
            Longitude = longitude;
            AllowedRadiusMeters = allowedRadiusMeters;
        }

        public bool IsWithinRange(double userLat, double userLon)
        {
            if (userLat < MinLatitude || userLat > MaxLatitude ||
                userLon < MinLongitude || userLon > MaxLongitude)
            {
                return false;
            }

            var dLat = ToRadians(userLat - Latitude);
            var dLon = ToRadians(userLon - Longitude);

            var lat1 = ToRadians(Latitude);
            var lat2 = ToRadians(userLat);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            var distance = EarthRadiusMeters * c;

            return distance <= AllowedRadiusMeters;
        }

        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Latitude;
            yield return Longitude;
            yield return AllowedRadiusMeters;
        }
    }
}