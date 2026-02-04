using DottIn.Application.Features.TimeKeepings.DTOs;
using MediatR;

namespace DottIn.Application.Features.TimeKeepings.Queries.GetCurrentTimeKeeping;

public record GetCurrentTimeKeepingQuery(Guid BranchId) : IRequest<IEnumerable<TimeKeepingSummaryDto>>;
