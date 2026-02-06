using DottIn.Application.Shared.DTOS;
using MediatR;

namespace DottIn.Application.Features.TimeKeepings.Commands.ClockIn;

public record ClockInCommand(Guid BranchId, Guid EmployeeId, GeolocationDto GeolocationDto) : IRequest<Guid>;
