namespace DottIn.Application.Features.TimeKeepings;

public static class TimeKeepingPeriod
{
    public const int MaximumDays = 366;

    public static DateOnly NormalizeAndValidate(DateOnly startDate, DateOnly? endDate)
    {
        var normalizedEnd = endDate ?? startDate.AddMonths(1).AddDays(-1);
        if (normalizedEnd < startDate)
            throw new ArgumentException("A data final não pode ser anterior à data inicial.");

        if (normalizedEnd.DayNumber - startDate.DayNumber + 1 > MaximumDays)
            throw new ArgumentException($"O período máximo permitido é de {MaximumDays} dias.");

        return normalizedEnd;
    }
}
