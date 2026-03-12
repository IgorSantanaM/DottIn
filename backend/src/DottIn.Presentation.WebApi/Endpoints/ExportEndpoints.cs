using System.Globalization;
using System.Text;
using DottIn.Application.Features.TimeKeepings.Queries.GetBranchTimeKeepingByPeriod;
using DottIn.Domain.Core.Data;
using DottIn.Domain.Employees;
using DottIn.Domain.Exports;
using DottIn.Presentation.WebApi.Endpoints.Internal;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DottIn.Presentation.WebApi.Endpoints;

public class ExportEndpoints : IEndpoint
{
    private const string Tag = "Exports";

    public static void DefineEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/api/branches/{branchId}")
            .WithTags(Tag)
            .RequireAuthorization();

        group.MapGet("/dominio-mappings", HandleGetMappingsAsync);
        group.MapPut("/dominio-mappings", HandleSaveMappingsAsync);
        group.MapGet("/exports/dominio", HandleExportDominioAsync);
        group.MapGet("/exports/csv", HandleExportCsvAsync);
    }

    private static async Task<IResult> HandleGetMappingsAsync(
        [FromRoute] Guid branchId,
        [FromServices] IDominioMappingRepository mappingRepo,
        [FromServices] IEmployeeRepository employeeRepo,
        CancellationToken cancellationToken)
    {
        var employees = await employeeRepo.GetByBranchIdAsync(branchId, cancellationToken);
        var mappings = await mappingRepo.GetByBranchAsync(branchId, cancellationToken);
        var mappingDict = mappings.ToDictionary(m => m.EmployeeId, m => m.DominioCode);

        var result = employees.Select(e => new DominioMappingDto(
            e.Id,
            e.Name,
            mappingDict.TryGetValue(e.Id, out var code) ? code : ""
        ));

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleSaveMappingsAsync(
        [FromRoute] Guid branchId,
        [FromBody] IEnumerable<SaveDominioMappingRequest> request,
        [FromServices] IDominioMappingRepository mappingRepo,
        [FromServices] IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var existingMappings = await mappingRepo.GetByBranchAsync(branchId, cancellationToken);
        var existingDict = existingMappings.ToDictionary(m => m.EmployeeId);

        foreach (var item in request)
        {
            if (string.IsNullOrWhiteSpace(item.DominioCode))
                continue;

            if (existingDict.TryGetValue(item.EmployeeId, out var existing))
            {
                existing.UpdateCode(item.DominioCode);
                await mappingRepo.UpdateAsync(existing);
            }
            else
            {
                var mapping = new DominioEmployeeMapping(item.EmployeeId, branchId, item.DominioCode);
                await mappingRepo.AddAsync(mapping, cancellationToken);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> HandleExportDominioAsync(
        [FromRoute] Guid branchId,
        [FromQuery] string month,
        [FromServices] IMediator mediator,
        [FromServices] IDominioMappingRepository mappingRepo,
        CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParseExact(month + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var firstDay))
            return Results.BadRequest("Formato de mês inválido. Use yyyy-MM.");

        var lastDay = firstDay.AddMonths(1).AddDays(-1);
        var period = firstDay.ToString("yyyyMM");

        var query = new GetBranchTimeKeepingByPeriodQuery(branchId, firstDay, lastDay);
        var records = await mediator.Send(query, cancellationToken);

        var mappings = await mappingRepo.GetByBranchAsync(branchId, cancellationToken);
        var codeMap = mappings.ToDictionary(m => m.EmployeeId, m => m.DominioCode);

        var sb = new StringBuilder();

        var grouped = records.GroupBy(r => r.EmployeeId);

        foreach (var group in grouped.OrderBy(g => codeMap.TryGetValue(g.Key, out var c) ? c : "9999999999"))
        {
            if (!codeMap.TryGetValue(group.Key, out var dominioCode))
                continue;

            // Domínio expects centesimal hours: hours * 100 (e.g., 8h30m = 8.50 = value 850)
            var totalWorkedHours = group.Sum(r => r.TotalWorked.TotalHours);
            var nocturnalHours = group.Where(r => r.IsNocturnal).Sum(r => r.TotalWorked.TotalHours);
            var holidayHours = group.Where(r => r.IsHoliday).Sum(r => r.TotalWorked.TotalHours);
            var overtimeHours = 0d;

            // Event 0001 - Regular hours (Horas Normais)
            if (totalWorkedHours > 0)
                sb.AppendLine(BuildDominioLine(dominioCode, period, "0001", 1, (long)Math.Round(totalWorkedHours * 100)));

            // Event 0042 - Overtime (Horas Extras)
            if (overtimeHours > 0)
                sb.AppendLine(BuildDominioLine(dominioCode, period, "0042", 1, (long)Math.Round(overtimeHours * 100)));

            // Event 0150 - Nocturnal hours (Adicional Noturno)
            if (nocturnalHours > 0)
                sb.AppendLine(BuildDominioLine(dominioCode, period, "0150", 1, (long)Math.Round(nocturnalHours * 100)));

            // Event 0250 - Holiday hours (Horas em Feriado)
            if (holidayHours > 0)
                sb.AppendLine(BuildDominioLine(dominioCode, period, "0250", 1, (long)Math.Round(holidayHours * 100)));
        }

        var content = sb.ToString();
        var bytes = Encoding.UTF8.GetBytes(content);
        return Results.File(bytes, "text/plain", $"dominio_{period}.txt");
    }

    private static async Task<IResult> HandleExportCsvAsync(
        [FromRoute] Guid branchId,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetBranchTimeKeepingByPeriodQuery(branchId, startDate, endDate);
        var records = await mediator.Send(query, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("Funcionário,Data,Entrada,Saída,Trabalhado,Intervalo,Noturno,Feriado,Nome Feriado,Status,Origem");

        foreach (var r in records.OrderBy(r => r.EmployeeName).ThenBy(r => r.WorkDate))
        {
            var line = string.Join(",",
                EscapeCsv(r.EmployeeName),
                r.WorkDate.ToString("dd/MM/yyyy"),
                r.ClockIn?.ToString("HH:mm") ?? "",
                r.ClockOut?.ToString("HH:mm") ?? "",
                r.TotalWorked.ToString(@"hh\:mm"),
                r.TotalBreak.ToString(@"hh\:mm"),
                r.IsNocturnal ? "Sim" : "Não",
                r.IsHoliday ? "Sim" : "Não",
                EscapeCsv(r.HolidayName ?? ""),
                r.Status,
                r.Source);
            sb.AppendLine(line);
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        var filename = $"registros_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv";
        return Results.File(bytes, "text/csv", filename);
    }

    private static string BuildDominioLine(string dominioCode, string period, string eventCode, int sign, long value)
    {
        // Format: [1:Type][10:Code][6:Period][4:Event][1:RefType=1][1:Sign][10:Value][10:Reference=0]
        return $"1{dominioCode}{period}{eventCode}1{sign}{value.ToString().PadLeft(10, '0')}{"".PadLeft(10, '0')}";
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}

public record DominioMappingDto(Guid EmployeeId, string EmployeeName, string DominioCode);
public record SaveDominioMappingRequest(Guid EmployeeId, string DominioCode);
