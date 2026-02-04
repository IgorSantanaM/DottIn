using DottIn.Domain.TimeKeepings;

namespace DottIn.Application.Features.TimeKeepings.DTOs;

public record TimeKeepingSummaryDto(TimeKeepingStatus Status, DateTime CreatedAt, int EntriesCount);
