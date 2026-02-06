using DottIn.Application.Shared.DTOS;
using MediatR;

namespace DottIn.Application.Features.TimeKeepings.Commands.ClockOut;

public record ClockOutCommand(Guid BranchId, Guid EmployeeId, GeolocationDto GeolocationDto) : IRequest<Unit>;