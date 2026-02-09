namespace DottIn.Presentation.WebApi.DTOs.Branches
{
    public record UpdateScheduleRequest(TimeOnly Start, TimeOnly End);
}
