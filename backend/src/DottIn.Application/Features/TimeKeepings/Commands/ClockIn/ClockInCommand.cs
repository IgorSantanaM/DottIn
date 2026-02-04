using DottIn.Application.Features.TimeKeepings.DTOs;
using MediatR;

namespace DottIn.Application.Features.TimeKeepings.Commands.ClockIn;

public record ClockInCommand(Guid BranchId, Guid EmployeeId, GeolocationDto GeolocationDto) : IRequest<Guid>;
