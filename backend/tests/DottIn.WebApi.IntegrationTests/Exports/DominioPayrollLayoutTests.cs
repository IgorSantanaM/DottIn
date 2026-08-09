using DottIn.Presentation.WebApi.Exports;

namespace DottIn.WebApi.IntegrationTests.Exports;

public sealed class DominioPayrollLayoutTests
{
    [Fact]
    public void BuildLaunchLine_MatchesOfficialDominioExample()
    {
        var line = DominioPayrollLayout.BuildLaunchLine(
            employeeCode: "88",
            period: "200605",
            rubricCode: "37",
            processType: "11",
            valueInHundredths: 3333,
            companyCode: "11");

        Assert.Equal("1000000000882006050037110000033330000000011", line);
        Assert.Equal(43, line.Length);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ABC")]
    [InlineData("12345678901")]
    public void NormalizeNumeric_RejectsInvalidEmployeeCode(string code)
    {
        Assert.Throws<ArgumentException>(() =>
            DominioPayrollLayout.NormalizeNumeric(code, 10, "Código do empregado"));
    }
}
