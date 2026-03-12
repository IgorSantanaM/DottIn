namespace DottIn.Presentation.WebApi.DTOs.TimeKeepings
{
    public record ClockOutRequest(
            Guid BranchId,
            Guid EmployeeId,
            double Latitude,
            double Longitude,
            bool SkipGeolocationValidation = false,
            string Source = "Mobile");
}
