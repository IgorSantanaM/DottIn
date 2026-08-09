using System.Globalization;
using System.Text;
using DottIn.Application.Features.TimeKeepings.Queries.GetBranchTimeKeepingByPeriod;
using DottIn.Domain.Core.Data;
using DottIn.Domain.Employees;
using DottIn.Domain.Exports;
using DottIn.Presentation.WebApi.Endpoints.Internal;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using DottIn.Presentation.WebApi.Security;
using DottIn.Presentation.WebApi.Exports;

namespace DottIn.Presentation.WebApi.Endpoints;

public class ExportEndpoints : IEndpoint
{
    private const string Tag = "Exports";

    public static void DefineEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/api/branches/{branchId}")
            .WithTags(Tag)
            .RequireAuthorization()
            .AddEndpointFilter<TenantAuthorizationFilter>();

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
            e.CPF.Value,
            mappingDict.TryGetValue(e.Id, out var code) ? code : ""
        ));

        return Results.Ok(result);
    }

    private static async Task<IResult> HandleSaveMappingsAsync(
        [FromRoute] Guid branchId,
        [FromBody] IEnumerable<SaveDominioMappingRequest> request,
        [FromServices] IDominioMappingRepository mappingRepo,
        [FromServices] IEmployeeRepository employeeRepo,
        [FromServices] IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var existingMappings = await mappingRepo.GetByBranchAsync(branchId, cancellationToken);
        var existingDict = existingMappings.ToDictionary(m => m.EmployeeId);
        var employees = await employeeRepo.GetByBranchIdAsync(branchId, cancellationToken);
        var employeeIds = employees.Select(employee => employee.Id).ToHashSet();
        var requestedMappings = request
            .Where(item => !string.IsNullOrWhiteSpace(item.DominioCode))
            .ToList();
        var desiredCodes = existingMappings.ToDictionary(mapping => mapping.EmployeeId, mapping => mapping.DominioCode);

        foreach (var item in requestedMappings)
        {
            if (!employeeIds.Contains(item.EmployeeId))
                return Results.BadRequest(new { Message = "Funcionário não pertence à filial informada." });

            try
            {
                desiredCodes[item.EmployeeId] = DominioPayrollLayout.NormalizeNumeric(
                    item.DominioCode,
                    10,
                    "Código do empregado");
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { Message = ex.Message });
            }
        }

        var duplicateCode = desiredCodes
            .GroupBy(mapping => mapping.Value)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateCode is not null)
            return Results.BadRequest(new { Message = $"O código Domínio {duplicateCode.Key.TrimStart('0')} está associado a mais de um funcionário." });

        foreach (var item in requestedMappings)
        {
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
        [FromQuery] string companyCode,
        [FromQuery] string normalRubricCode,
        [FromQuery] string nocturnalRubricCode,
        [FromQuery] string holidayRubricCode,
        [FromQuery] string processType,
        [FromServices] IMediator mediator,
        [FromServices] IDominioMappingRepository mappingRepo,
        CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParseExact(month + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var firstDay))
            return Results.BadRequest("Formato de mês inválido. Use yyyy-MM.");

        try
        {
            companyCode = DominioPayrollLayout.NormalizeNumeric(companyCode, 10, "Código da empresa");
            normalRubricCode = DominioPayrollLayout.NormalizeNumeric(normalRubricCode, 4, "Rubrica de horas normais");
            nocturnalRubricCode = DominioPayrollLayout.NormalizeNumeric(nocturnalRubricCode, 4, "Rubrica de adicional noturno");
            holidayRubricCode = DominioPayrollLayout.NormalizeNumeric(holidayRubricCode, 4, "Rubrica de horas em feriado");
            processType = DominioPayrollLayout.NormalizeNumeric(processType, 2, "Tipo do processo");
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { Message = ex.Message });
        }

        var lastDay = firstDay.AddMonths(1).AddDays(-1);
        var period = firstDay.ToString("yyyyMM");

        var query = new GetBranchTimeKeepingByPeriodQuery(branchId, firstDay, lastDay);
        var records = await mediator.Send(query, cancellationToken);

        var mappings = await mappingRepo.GetByBranchAsync(branchId, cancellationToken);
        var codeMap = mappings.ToDictionary(m => m.EmployeeId, m => m.DominioCode);

        var unmappedEmployees = records
            .Where(record => !codeMap.ContainsKey(record.EmployeeId))
            .Select(record => record.EmployeeName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name)
            .ToList();
        if (unmappedEmployees.Count > 0)
        {
            return Results.BadRequest(new
            {
                Message = "Existem funcionários com registros sem código Domínio: " + string.Join(", ", unmappedEmployees)
            });
        }

        var sb = new StringBuilder();

        var grouped = records.GroupBy(r => r.EmployeeId);

        foreach (var group in grouped.OrderBy(g => codeMap.TryGetValue(g.Key, out var c) ? c : "9999999999"))
        {
            if (!codeMap.TryGetValue(group.Key, out var dominioCode))
                continue;

            var totalWorkedHours = group.Sum(r => r.TotalWorked.TotalHours);
            var nocturnalHours = group.Where(r => r.IsNocturnal).Sum(r => r.TotalWorked.TotalHours);
            var holidayHours = group.Where(r => r.IsHoliday).Sum(r => r.TotalWorked.TotalHours);
            // Regular hours
            if (totalWorkedHours > 0)
                sb.AppendLine(DominioPayrollLayout.BuildLaunchLine(dominioCode, period, normalRubricCode, processType, (long)Math.Round(totalWorkedHours * 100), companyCode));

            // Nocturnal hours
            if (nocturnalHours > 0)
                sb.AppendLine(DominioPayrollLayout.BuildLaunchLine(dominioCode, period, nocturnalRubricCode, processType, (long)Math.Round(nocturnalHours * 100), companyCode));

            // Holiday hours
            if (holidayHours > 0)
                sb.AppendLine(DominioPayrollLayout.BuildLaunchLine(dominioCode, period, holidayRubricCode, processType, (long)Math.Round(holidayHours * 100), companyCode));
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

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}

public record DominioMappingDto(Guid EmployeeId, string EmployeeName, string EmployeeDocument, string DominioCode);
public record SaveDominioMappingRequest(Guid EmployeeId, string DominioCode);
