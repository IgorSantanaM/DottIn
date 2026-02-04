using DottIn.Application.Features.TimeKeepings.DTOs;
using MediatR;

namespace DottIn.Application.Features.TimeKeepings.Queries.GetAllTimeKeepings;

public record GetTimeKeepingByPeriodQuery(Guid EmployeeId,
                                    DateOnly StartDate,
                                    DateOnly? EndDate)
                                    : IRequest<IEnumerable<TimeKeepingSummaryDto>>; // TODO: Pagination
