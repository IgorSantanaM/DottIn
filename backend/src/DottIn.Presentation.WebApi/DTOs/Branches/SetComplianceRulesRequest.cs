namespace DottIn.Presentation.WebApi.DTOs.Branches
{
    public record SetComplianceRulesRequest(int ToleranceMinutes, Guid? HolidayCalendarId);
}
