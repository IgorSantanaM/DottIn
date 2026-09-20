namespace DottIn.Presentation.WebApi.DTOs.Branches;

public record BranchClockResponse(DateTime UtcNow, DateTime LocalNow, string TimeZoneId);
