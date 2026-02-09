using MediatR;

namespace DottIn.Application.Features.Employees.Commands.UpdateProfile;

public record UpdateProfileCommand(Guid EmployeeId,
    Guid BranchId,
    string Name,
    Stream? EmployeeImage,
    string? ImageContentType) : IRequest<Unit>;
