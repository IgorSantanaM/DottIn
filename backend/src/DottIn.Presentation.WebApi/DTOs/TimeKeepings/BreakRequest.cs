namespace DottIn.Presentation.WebApi.DTOs.TimeKeepings
{
    public record BreakRequest(
        Guid BranchId,
        Guid EmployeeId,
        double Latitude,
        double Longitude);

}
