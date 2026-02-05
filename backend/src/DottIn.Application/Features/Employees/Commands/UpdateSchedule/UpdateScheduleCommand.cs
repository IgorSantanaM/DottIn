using MediatR;

namespace DottIn.Application.Features.Employees.Commands.UpdateSchedule;

public record UpdateScheduleCommand(Guid BranchId,
                Guid EmployeeId,
                TimeOnly Start,
                TimeOnly End,
                TimeOnly IntervalStart,
                TimeOnly IntervalEnd) : IRequest<bool>;
