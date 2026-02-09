namespace DottIn.Presentation.WebApi.DTOs.Employees;

public record UpdateProfileRequest(
    string Name,
    IFormFile? Image);
