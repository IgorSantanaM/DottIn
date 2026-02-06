using DottIn.Application.Shared.DTOS;
using MediatR;

namespace DottIn.Application.Features.TimeKeepings.Commands.Break;

public record BreakCommand(Guid EmployeeId, Guid BranchId, GeolocationDto GeolocationDto) : IRequest<Unit>;
