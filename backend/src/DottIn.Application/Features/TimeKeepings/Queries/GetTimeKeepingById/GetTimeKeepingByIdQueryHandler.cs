using DottIn.Application.Exceptions;
using DottIn.Application.Features.TimeKeepings.DTOs;
using DottIn.Application.Shared.DTOS;
using DottIn.Domain.Branches;
using DottIn.Domain.Employees;
using DottIn.Domain.TimeKeepings;
using MediatR;

namespace DottIn.Application.Features.TimeKeepings.Queries.GetTimeKeepingById
{
    public class GetTimeKeepingByIdQueryHandler(
        ITimeKeepingRepository timeKeepingRepository,
        IBranchRepository branchRepository,
        IEmployeeRepository employeeRepository)
        : IRequestHandler<GetTimeKeepingByIdQuery, TimeKeepingDetailsDto>
    {
        public async Task<TimeKeepingDetailsDto> Handle(GetTimeKeepingByIdQuery request, CancellationToken cancellationToken)
        {
            var timeKeeping = await timeKeepingRepository.GetByIdAsync(request.TimeKeepingId, cancellationToken);

            if (timeKeeping is null)
                throw NotFoundException.ForEntity(nameof(TimeKeeping), request.TimeKeepingId);

            var branch = await branchRepository.GetByIdAsync(timeKeeping.BranchId, cancellationToken);

            if (branch is null)
                throw NotFoundException.ForEntity(nameof(Branch), timeKeeping.BranchId);

            var employee = await employeeRepository.GetByIdAsync(timeKeeping.EmployeeId, cancellationToken);

            if (employee is null)
                throw NotFoundException.ForEntity(nameof(Employee), timeKeeping.EmployeeId);

            GeolocationDto geolocationDto = new(timeKeeping.Location!.Latitude, timeKeeping.Location.Longitude);

            TimeKeepingDetailsDto timeKeepingDetailsDto = new(employee.Name,
                                                        branch.Name,
                                                        timeKeeping.Status,
                                                        timeKeeping.WorkDate,
                                                        timeKeeping.CreatedAt,
                                                        geolocationDto,
                                                        timeKeeping.Entries.Select(tke => new TimeEntryDto(tke.Timestamp, tke.Type)));

            return timeKeepingDetailsDto;
        }
    }
}
