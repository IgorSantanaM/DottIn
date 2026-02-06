using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace DottIn.Application.Features.Branches.Commands.SetComplianceRule
{
    public record SetComplianceRulesCommand(Guid BranchId,
        int ToleranceMinutes, 
        Guid? HolidayCalendarId) 
        : IRequest<Unit>;
}
