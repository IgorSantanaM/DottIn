using MediatR;

namespace DottIn.Application.Features.Branches.Commands.UpdateConfig;

public record UpdateConfigCommand(Guid BranchId, 
    int AllowedRadiusMeters, 
    string TimeZoneId) : IRequest<Unit>;
