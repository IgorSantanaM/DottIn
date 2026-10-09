namespace DottIn.Admin.Services;

public readonly record struct PayrollPeriod(DateOnly Start, DateOnly End)
{
    public static PayrollPeriod FromMonth(DateTime date)
        => new(new DateOnly(date.Year, date.Month, 1),
            new DateOnly(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month)));
}
