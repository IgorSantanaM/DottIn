using MediatR;

namespace DottIn.Application.Features.Branches.Commands.UpdateSchedule;

public record UpdateScheduleCommand(Guid BranchId, TimeOnly Start, TimeOnly End) : IRequest<Unit>;
