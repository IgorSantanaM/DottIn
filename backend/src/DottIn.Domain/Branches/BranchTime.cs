using DottIn.Domain.Core.Exceptions;

namespace DottIn.Domain.Branches;

/// <summary>
/// Centraliza as conversões entre UTC e o fuso configurado para uma filial.
/// Timestamps persistidos devem sempre estar em UTC.
/// </summary>
public static class BranchTime
{
    public static DateTime NormalizeUtc(DateTime timestamp)
    {
        if (timestamp.Kind != DateTimeKind.Utc)
            throw new DomainException("O horário informado deve estar em UTC.");

        return timestamp;
    }

    public static DateTime ToLocal(DateTime utcTimestamp, string timeZoneId)
    {
        NormalizeUtc(utcTimestamp);
        var timeZone = Resolve(timeZoneId);
        return DateTime.SpecifyKind(
            TimeZoneInfo.ConvertTimeFromUtc(utcTimestamp, timeZone),
            DateTimeKind.Unspecified);
    }

    public static DateOnly GetLocalDate(DateTime utcTimestamp, string timeZoneId)
        => DateOnly.FromDateTime(ToLocal(utcTimestamp, timeZoneId));

    public static TimeZoneInfo Resolve(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new DomainException("O fuso horário da filial não foi configurado.");

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new DomainException($"Fuso horário inválido: {timeZoneId}");
        }
        catch (InvalidTimeZoneException)
        {
            throw new DomainException($"Fuso horário inválido: {timeZoneId}");
        }
    }
}
