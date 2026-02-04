using DottIn.Application.Features.TimeKeepings.DTOs;
using DottIn.Domain.TimeKeepings;
using FluentValidation;
using MediatR;

namespace DottIn.Application.Features.TimeKeepings.Queries.GetCurrentTimeKeeping
{
    public class GetCurrentTimeKeepingQueryHandler(ITimeKeepingRepository timeKeepingRepository) : IRequestHandler<GetCurrentTimeKeepingQuery, IEnumerable<TimeKeepingSummaryDto>>
    {
        public async Task<IEnumerable<TimeKeepingSummaryDto>> Handle(GetCurrentTimeKeepingQuery request, CancellationToken cancellationToken)
        {
            if (request.BranchId == Guid.Empty)
                throw new ValidationException("Id da empresa não pode ser nulo.");

            var timeKeepings = await timeKeepingRepository.GetActiveByBranchAsync(request.BranchId, cancellationToken);

            IEnumerable<TimeKeepingSummaryDto> timeKeepingSummariesDto = timeKeepings
                .Select(tk => new TimeKeepingSummaryDto(tk.Status, tk.CreatedAt, tk.Entries.Count));

            return timeKeepingSummariesDto;
        }
    }
}
