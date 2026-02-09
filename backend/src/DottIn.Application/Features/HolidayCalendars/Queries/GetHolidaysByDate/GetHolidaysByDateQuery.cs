using DottIn.Application.Features.Branches.DTOs;
using DottIn.Application.Features.HolidayCalendars.DTOs;
using MediatR;

namespace DottIn.Application.Features.HolidayCalendars.Queries.GetHolidaysByDate;

public record GetHolidaysByDateQuery(Guid BranchId, DateOnly Date) : IRequest<HolidayDto>;
