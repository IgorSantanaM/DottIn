using DottIn.Application.Exceptions;
using DottIn.Application.Features.TimeKeepings.DTOs;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Employees;
using DottIn.Domain.TimeKeepings;
using MediatR;

namespace DottIn.Application.Features.TimeKeepings.Queries.GetTimeKeepingByPeriod
{
    public class GetTimeKeepingByPeriodQueryHandler(ITimeKeepingRepository timeKeepingRepository,
        IEmployeeRepository employeeRepository,
        IBranchRepository branchRepository)
        : IRequestHandler<GetTimeKeepingByPeriodQuery, IEnumerable<TimeKeepingRecordDto>>
    {
        public async Task<IEnumerable<TimeKeepingRecordDto>> Handle(GetTimeKeepingByPeriodQuery request, CancellationToken cancellationToken)
        {
            var employee = await employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);

            if (employee is null)
                throw NotFoundException.ForEntity(nameof(Employee), request.EmployeeId);

            if (!employee.IsActive)
                throw new DomainException("O Funcionário não esta ativo.");

            var branch = await branchRepository.GetByIdAsync(employee.BranchId, cancellationToken);

            if (branch is null)
                throw NotFoundException.ForEntity(nameof(Branch), employee.BranchId);

            if (!branch.IsActive)
                throw new DomainException("A Empresa não esta ativa.");

            var timeKeepings = await timeKeepingRepository
                .GetByEmployeeAndPeriodAsync(request.EmployeeId, request.StartDate, request.EndDate);

            var records = timeKeepings.Select(tk =>
            {
                var clockIn = tk.Entries.FirstOrDefault(e => e.Type == TimeKeepingType.ClockIn)?.Timestamp;
                var clockOut = tk.Entries.FirstOrDefault(e => e.Type == TimeKeepingType.ClockOut)?.Timestamp;

                var totalWorked = clockIn.HasValue && clockOut.HasValue && clockOut > clockIn
                    ? clockOut.Value - clockIn.Value
                    : TimeSpan.Zero;

                var breaks = tk.Entries
                    .Where(e => e.Type == TimeKeepingType.BreakStart || e.Type == TimeKeepingType.BreakEnd)
                    .OrderBy(e => e.Timestamp)
                    .ToList();

                var totalBreak = TimeSpan.Zero;
                for (int i = 0; i < breaks.Count - 1; i += 2)
                {
                    if (breaks[i].Type == TimeKeepingType.BreakStart && breaks[i + 1].Type == TimeKeepingType.BreakEnd)
                        totalBreak += breaks[i + 1].Timestamp - breaks[i].Timestamp;
                }

                if (totalWorked > TimeSpan.Zero)
                    totalWorked -= totalBreak;

                return new TimeKeepingRecordDto(
                    tk.Id,
                    tk.WorkDate,
                    clockIn,
                    clockOut,
                    totalWorked,
                    totalBreak,
                    tk.Status.ToString());
            });

            return records;
        }
    }
}
