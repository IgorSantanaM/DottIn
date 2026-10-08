using System.Globalization;
using System.Text;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.Payrolls;

namespace DottIn.Application.Features.Payrolls;

public static class PayrollCsvExporter
{
    public static byte[] Export(Payroll payroll, IReadOnlyCollection<PayrollItem> items)
    {
        if (payroll.Status is not (PayrollStatus.ReadyForAccounting or PayrollStatus.Exported))
            throw new DomainException("Finalize a folha antes de exportar.");
        if (items.Count == 0)
            throw new DomainException("A folha não possui colaboradores para exportar.");
        var unmapped = items.Where(x => string.IsNullOrWhiteSpace(x.DominioCode))
            .Select(x => x.EmployeeName).OrderBy(x => x).ToArray();
        if (unmapped.Length > 0)
            throw new DomainException($"Informe o código Domínio de: {string.Join(", ", unmapped)}.");
        if (items.Any(x => x.PaymentAmount is null || string.IsNullOrWhiteSpace(x.EmployeeName)))
            throw new DomainException("Há colaboradores sem nome ou valor aprovado.");

        var csv = new StringBuilder("NOME;ID_DOMINIO;SALARIO\r\n");
        foreach (var item in items.OrderBy(x => x.EmployeeName).ThenBy(x => x.EmployeeId))
        {
            csv.Append(Escape(item.EmployeeName)).Append(';')
                .Append(Escape(item.DominioCode!)).Append(';')
                .Append(item.PaymentAmount!.Value.ToString("F2", CultureInfo.GetCultureInfo("pt-BR")))
                .Append("\r\n");
        }
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
    }

    private static string Escape(string value)
    {
        // Spreadsheet clients can execute formulas in cells, even when a CSV field is quoted.
        if (value.Length > 0 && "=+-@".Contains(value[0]))
            throw new DomainException("Nome ou código Domínio não pode iniciar com um operador de fórmula.");
        return value.IndexOfAny([';', '"', '\r', '\n']) >= 0
            ? '"' + value.Replace("\"", "\"\"") + '"'
            : value;
    }
}
