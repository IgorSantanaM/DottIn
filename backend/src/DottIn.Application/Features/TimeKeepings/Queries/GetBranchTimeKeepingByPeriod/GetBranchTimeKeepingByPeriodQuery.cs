using DottIn.Application.Features.TimeKeepings.DTOs;
using MediatR;

namespace DottIn.Application.Features.TimeKeepings.Queries.GetBranchTimeKeepingByPeriod;

public record GetBranchTimeKeepingByPeriodQuery(
    Guid BranchId,
    DateOnly StartDate,
    DateOnly? EndDate)
    : IRequest<IEnumerable<BranchTimeKeepingRecordDto>>;
