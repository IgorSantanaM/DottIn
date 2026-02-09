using MediatR;

namespace DottIn.Application.Features.Branches.Commands.SetComplianceRules
{
    public record SetComplianceRulesCommand(Guid BranchId,
        int ToleranceMinutes,
        Guid? HolidayCalendarId)
        : IRequest<Unit>;
}
