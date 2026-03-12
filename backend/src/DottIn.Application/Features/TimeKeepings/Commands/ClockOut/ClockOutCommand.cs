using DottIn.Application.Shared.DTOS;
using DottIn.Domain.TimeKeepings;
using MediatR;

namespace DottIn.Application.Features.TimeKeepings.Commands.ClockOut;

public record ClockOutCommand(Guid BranchId, Guid EmployeeId, GeolocationDto GeolocationDto, bool SkipGeolocationValidation = false, ClockSource Source = ClockSource.Mobile) : IRequest<Unit>;