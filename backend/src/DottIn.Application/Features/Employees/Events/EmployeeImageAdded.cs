using DottIn.Domain.Core.Models;

namespace DottIn.Domain.Employees.Events;

public record EmployeeImageAdded(Guid EmployeeId,
                Stream ImageStream,
                string ImageName,
                string ImageContentType) : Event<Guid>(EmployeeId);
