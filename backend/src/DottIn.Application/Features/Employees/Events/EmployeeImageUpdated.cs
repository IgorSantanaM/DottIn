using DottIn.Domain.Core.Models;

namespace DottIn.Application.Features.Employees.Events;

public record EmployeeImageUpdated(
    Guid EmployeeId,
    byte[] ImageData,
    string ImageName,
    string ImageContentType) : Event<Guid>(EmployeeId);
