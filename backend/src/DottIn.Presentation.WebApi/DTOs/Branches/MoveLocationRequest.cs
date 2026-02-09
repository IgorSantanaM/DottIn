using DottIn.Application.Features.Branches.DTOs;
using DottIn.Application.Shared.DTOS;

namespace DottIn.Presentation.WebApi.DTOs.Branches
{
    public record MoveLocationRequest(
         AddressDto NewAddress,
         GeolocationDto NewGeolocation,
         string? NewTimeZoneId);
}
