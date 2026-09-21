namespace DottIn.Application.Shared.DTOS;

public record GeolocationDto(
    double Latitude,
    double Longitude,
    double? AccuracyMeters = null,
    DateTime? CapturedAtUtc = null);
