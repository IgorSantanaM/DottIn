namespace DottIn.Presentation.WebApi.DTOs.TimeKeepings
{
    public record ClockInRequest(
         Guid BranchId,
         Guid EmployeeId,
         double Latitude,
         double Longitude);
}
