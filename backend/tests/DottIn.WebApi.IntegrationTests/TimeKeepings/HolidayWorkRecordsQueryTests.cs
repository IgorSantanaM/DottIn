using System.Reflection;
using DottIn.Application.Features.TimeKeepings.Queries.GetHolidayWorkRecords;
using DottIn.Domain.Branches;
using DottIn.Domain.Employees;
using DottIn.Domain.HolidayCalendars;
using DottIn.Domain.TimeKeepings;
using DottIn.Domain.ValueObjects;

namespace DottIn.WebApi.IntegrationTests.TimeKeepings;

public sealed class HolidayWorkRecordsQueryTests
{
    [Fact]
    public async Task HandlerReadsOnlyHolidayDatesAndReturnsCompactMetrics()
    {
        var branch = CreateBranch();
        var workDate = DateOnly.FromDateTime(BranchTime.ToLocal(DateTime.UtcNow, branch.TimeZoneId));
        var location = new Geolocation(-20.45, -54.62);
        var clockIn = DateTime.UtcNow.AddHours(-3);
        var record = new TimeKeeping(branch.Id, Guid.NewGuid(), location, workDate, branch.TimeZoneId, clockIn);
        record.ClockIn(clockIn);
        record.ClockOut(clockIn.AddHours(2));
        var selectedDates = Array.Empty<DateOnly>();

        var handler = new GetHolidayWorkRecordsQueryHandler(
            Proxy<IBranchRepository>((method, _) => method.Name == "GetByIdAsync"
                ? Task.FromResult<Branch?>(branch) : throw new NotSupportedException(method.Name)),
            Proxy<IHolidayCalendarRepository>((method, _) => method.Name == "GetHolidaysInRangeAsync"
                ? Task.FromResult<IEnumerable<Holiday>>([new Holiday(workDate, "Feriado", HolidayType.National)])
                : throw new NotSupportedException(method.Name)),
            Proxy<ITimeKeepingRepository>((method, args) =>
            {
                if (method.Name != "GetByBranchAndDatesAsync")
                    throw new NotSupportedException(method.Name);
                selectedDates = ((IReadOnlyCollection<DateOnly>)args![1]!).ToArray();
                return Task.FromResult<IReadOnlyList<TimeKeeping>>([record]);
            }),
            Proxy<ITimeKeepingAdjustmentRepository>((method, _) => method.Name == "GetApprovedByTimeKeepingIdsAsync"
                ? Task.FromResult<IEnumerable<TimeKeepingAdjustment>>([])
                : throw new NotSupportedException(method.Name)),
            Proxy<IEmployeeRepository>((method, _) => method.Name == "GetNamesByIdsAsync"
                ? Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string> { [record.EmployeeId] = "Ana" })
                : throw new NotSupportedException(method.Name)));

        var result = await handler.Handle(
            new GetHolidayWorkRecordsQuery(branch.Id, workDate.Year),
            TestContext.Current.CancellationToken);

        Assert.Equal([workDate], selectedDates);
        var item = Assert.Single(result);
        Assert.Equal("Ana", item.EmployeeName);
        Assert.Equal("Feriado", item.HolidayName);
        Assert.Equal(TimeSpan.FromHours(2), item.TotalWorked);
    }

    [Fact]
    public async Task NoHolidaysAvoidsAttendanceLookup()
    {
        var branch = CreateBranch();
        var handler = new GetHolidayWorkRecordsQueryHandler(
            Proxy<IBranchRepository>((method, _) => method.Name == "GetByIdAsync"
                ? Task.FromResult<Branch?>(branch) : throw new NotSupportedException(method.Name)),
            Proxy<IHolidayCalendarRepository>((method, _) => method.Name == "GetHolidaysInRangeAsync"
                ? Task.FromResult<IEnumerable<Holiday>>([]) : throw new NotSupportedException(method.Name)),
            Proxy<ITimeKeepingRepository>((method, _) => throw new InvalidOperationException("Attendance lookup must not run.")),
            Proxy<ITimeKeepingAdjustmentRepository>((method, _) => throw new NotSupportedException(method.Name)),
            Proxy<IEmployeeRepository>((method, _) => throw new NotSupportedException(method.Name)));

        var result = await handler.Handle(
            new GetHolidayWorkRecordsQuery(branch.Id, DateTime.UtcNow.Year),
            TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    private static Branch CreateBranch() => new(
        "DottIn Teste",
        new Document("11.222.333/0001-81"),
        new Geolocation(-20.45, -54.62),
        new Address("Rua Teste", 10, "Campo Grande", "MS", "79000000"),
        "America/Campo_Grande",
        new TimeOnly(8, 0),
        new TimeOnly(18, 0),
        Guid.NewGuid(),
        email: "teste@dottin.local");

    private static T Proxy<T>(Func<MethodInfo, object?[]?, object?> handler) where T : class
    {
        var proxy = DispatchProxy.Create<T, RepositoryProxy>();
        ((RepositoryProxy)(object)proxy).Handler = handler;
        return proxy;
    }

    public class RepositoryProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = default!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            => Handler(targetMethod!, args);
    }
}