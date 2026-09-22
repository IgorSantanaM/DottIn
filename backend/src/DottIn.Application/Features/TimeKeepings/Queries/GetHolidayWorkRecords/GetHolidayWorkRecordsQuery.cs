using MediatR;

namespace DottIn.Application.Features.TimeKeepings.Queries.GetHolidayWorkRecords;

public sealed record GetHolidayWorkRecordsQuery(Guid BranchId, int Year)
    : IRequest<IReadOnlyList<HolidayWorkRecordDto>>;

public sealed record HolidayWorkRecordDto(
    string EmployeeName,
    DateOnly WorkDate,
    string HolidayName,
    TimeSpan TotalWorked);