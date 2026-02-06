using DottIn.Application.Features.Branches.DTOs;
using DottIn.Application.Shared.DTOS;
using MediatR;

namespace DottIn.Application.Features.Branches.Commands.CreateBranch;

public record CreateBranchCommand(string Name,
                DocumentDto Document, 
                GeolocationDto Geolocation,
                AddressDto Address, 
                string TimeZoneId, 
                TimeOnly StartWorkTime,
                TimeOnly EndWorkTime,
                string Email, 
                string PhoneNumber,
                Guid OwnerId, 
                bool IsHeadQuarters, 
                int AllowedRadiusMeters, 
                int ToleranceMinutes)
                : IRequest<Guid>;
