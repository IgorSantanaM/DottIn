using DottIn.Domain.TimeKeepings;

namespace DottIn.Application.Features.TimeKeepings.DTOs;

public record TimeKeepingDetailsDto(
                string EmployeeName,
                string BranchName,
                TimeKeepingStatus Status,
                DateOnly WorkDate,
                DateTime CreatedAt,
                GeolocationDto? GeolocationDto,
                IEnumerable<TimeEntryDto> EntriesDto);

