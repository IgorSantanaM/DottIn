using DottIn.Application.Features.Branches.DTOs;
using DottIn.Application.Shared.DTOS;
using MediatR;

namespace DottIn.Application.Features.Branches.Commands.MoveLocation
{
    public record MoveLocationCommand(Guid BranchId,
                                AddressDto NewAddress,
                                GeolocationDto NewGeolocation,
                                string? NewTimeZoneId) : IRequest<Unit>;
}
