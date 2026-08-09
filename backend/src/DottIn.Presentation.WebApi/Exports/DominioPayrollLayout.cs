namespace DottIn.Presentation.WebApi.Exports;

public static class DominioPayrollLayout
{
    public const int LaunchRecordLength = 43;

    public static string BuildLaunchLine(
        string employeeCode,
        string period,
        string rubricCode,
        string processType,
        long valueInHundredths,
        string companyCode)
    {
        var normalizedEmployee = NormalizeNumeric(employeeCode, 10, "Código do empregado");
        var normalizedPeriod = NormalizeNumeric(period, 6, "Competência");
        var normalizedRubric = NormalizeNumeric(rubricCode, 4, "Código da rubrica");
        var normalizedProcess = NormalizeNumeric(processType, 2, "Tipo do processo");
        var normalizedCompany = NormalizeNumeric(companyCode, 10, "Código da empresa");

        if (valueInHundredths is < 0 or > 999_999_999)
            throw new ArgumentOutOfRangeException(nameof(valueInHundredths), "O valor deve ocupar no máximo 9 posições.");

        var line = $"10{normalizedEmployee}{normalizedPeriod}{normalizedRubric}{normalizedProcess}{valueInHundredths:000000000}{normalizedCompany}";
        if (line.Length != LaunchRecordLength)
            throw new InvalidOperationException("A linha gerada não possui as 43 posições exigidas pelo Domínio.");

        return line;
    }

    public static string NormalizeNumeric(string value, int length, string fieldName)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length == 0 || trimmed.Length > length || !trimmed.All(char.IsDigit))
            throw new ArgumentException($"{fieldName} deve conter de 1 a {length} dígitos numéricos.", fieldName);

        return trimmed.PadLeft(length, '0');
    }
}
