using DottIn.Application.Shared.DTOS;
using DottIn.Domain.TimeKeepings;
using MediatR;

namespace DottIn.Application.Features.TimeKeepings.Commands.Break;

public record BreakCommand(Guid EmployeeId, Guid BranchId, GeolocationDto GeolocationDto, bool SkipGeolocationValidation = false, ClockSource Source = ClockSource.Mobile) : IRequest<Unit>;
