using System.Text;
using DottIn.Application.Features.Payrolls;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Payrolls;
using DottIn.Domain.TimeKeepings;

namespace DottIn.WebApi.IntegrationTests.Payrolls;

public sealed class PayrollCalculationAndCsvTests
{
    private static readonly DateTime Start = new(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Hours_ExcludeOtherMonthBranchAndEmployeeAndFlagOpenRecords()
    {
        var branch = Guid.NewGuid();
        var otherBranch = Guid.NewGuid();
        var employee = Guid.NewGuid();
        var otherEmployee = Guid.NewGuid();
        var valid = Record(branch, employee, new DateOnly(2026, 10, 4));
        valid.ClockIn(Start); valid.StartBreak(Start.AddHours(4));
        valid.EndBreak(Start.AddHours(4.5)); valid.ClockOut(Start.AddHours(8));
        var otherMonth = Record(branch, employee, new DateOnly(2026, 9, 4));
        otherMonth.ClockIn(Start); otherMonth.ClockOut(Start.AddHours(7));
        var otherBranchRecord = Record(otherBranch, employee, new DateOnly(2026, 10, 4));
        otherBranchRecord.ClockIn(Start); otherBranchRecord.ClockOut(Start.AddHours(7));
        var open = Record(branch, otherEmployee, new DateOnly(2026, 10, 4));
        open.ClockIn(Start);

        var hours = PayrollHoursCalculator.Calculate(branch, 2026, 10,
            [valid, otherMonth, otherBranchRecord, open], []);

        Assert.Equal(450, hours[employee].WorkedMinutes);
        Assert.False(hours[employee].HasIncompleteRecords);
        Assert.Equal(0, hours[otherEmployee].WorkedMinutes);
        Assert.True(hours[otherEmployee].HasIncompleteRecords);
    }

    [Fact]
    public void Csv_UsesApprovedDecimalAndUtf8Bom()
    {
        var owner = Guid.NewGuid();
        var payroll = new Payroll(Guid.NewGuid(), 2026, 10, owner, Start);
        var line = new PayrollItem(payroll.Id, Guid.NewGuid(), "João da Silva", "0000012345", 10110);
        line.SetPayment(3500m, null);
        payroll.Close(owner, Start, [line]);

        var bytes = PayrollCsvExporter.Export(payroll, [line]);
        Assert.True(bytes.AsSpan().StartsWith(Encoding.UTF8.GetPreamble()));
        Assert.Equal("NOME;ID_DOMINIO;SALARIO\r\nJoão da Silva;0000012345;3500,00\r\n",
            Encoding.UTF8.GetString(bytes, Encoding.UTF8.GetPreamble().Length,
                bytes.Length - Encoding.UTF8.GetPreamble().Length));
    }

    [Fact]
    public void Csv_RejectsDraftAndMissingMapping()
    {
        var owner = Guid.NewGuid();
        var payroll = new Payroll(Guid.NewGuid(), 2026, 10, owner, Start);
        var line = new PayrollItem(payroll.Id, Guid.NewGuid(), "Maria", null, 0);
        line.SetPayment(0m, null);
        Assert.Throws<DomainException>(() => PayrollCsvExporter.Export(payroll, [line]));
        Assert.Throws<DomainException>(() => payroll.Close(owner, Start, [line]));
    }

    private static TimeKeeping Record(Guid branch, Guid employee, DateOnly workDate)
        => new(branch, employee, new Geolocation(-15.6, -56.1), workDate, "UTC", Start);
}
