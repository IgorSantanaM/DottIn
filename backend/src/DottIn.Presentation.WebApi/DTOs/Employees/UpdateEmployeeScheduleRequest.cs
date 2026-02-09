namespace DottIn.Presentation.WebApi.DTOs.Employees
{
    public record UpdateEmployeeScheduleRequest(
        TimeOnly Start,
        TimeOnly End,
        TimeOnly IntervalStart,
        TimeOnly IntervalEnd);
}
