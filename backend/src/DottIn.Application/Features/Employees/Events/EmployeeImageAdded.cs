using DottIn.Domain.Core.Models;

namespace DottIn.Application.Features.Employees.Events;

public record EmployeeImageAdded(Guid EmployeeId,
                Stream ImageStream,
                string ImageName,
                string ImageContentType) : Event<Guid>(EmployeeId);
