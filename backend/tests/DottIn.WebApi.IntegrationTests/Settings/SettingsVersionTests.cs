using DottIn.Domain.Branches;
using DottIn.Domain.Employees;
using DottIn.Domain.ValueObjects;
using DottIn.Presentation.WebApi.DTOs.Settings;

namespace DottIn.WebApi.IntegrationTests.Settings;

public sealed class SettingsVersionTests
{
    [Fact]
    public void UnitVersionChangesWhenExistingOperationsChangeItsFields()
    {
        var branch = new Branch("Matriz", new("11222333000181"), new(-15, -56),
            new("Rua", 1, "Cuiabá", "MT", "78000000"), "America/Cuiaba", new(8, 0), new(17, 0),
            Guid.NewGuid(), email: "test@example.invalid");
        var original = UnitSettings.From(branch);
        Assert.Equal(original, UnitSettings.From(branch));
        branch.UpdateConfig(200, "America/Sao_Paulo");
        var updated = UnitSettings.From(branch);
        Assert.NotEqual(original.Version, updated.Version);
        Assert.Equal("America/Sao_Paulo", updated.TimeZoneId);
        Assert.Equal(200, updated.AllowedRadiusMeters);
    }

    [Fact]
    public void ProfileContractDoesNotExposeCredentialHashesOrSessionVersion()
    {
        var employee = new Employee("Ana", new Document("10020030088"), "StrongPass123!");
        var json = System.Text.Json.JsonSerializer.Serialize(PersonalSettings.From(employee));
        Assert.DoesNotContain("Hash", json);
        Assert.DoesNotContain(employee.SessionVersion.ToString(), json);
        var version = PersonalSettings.From(employee).Version;
        employee.UpdateProfile("Ana Silva");
        Assert.NotEqual(version, PersonalSettings.From(employee).Version);
    }

    [Fact]
    public void VersionsUsePostgresTimestampPrecision()
    {
        var time = new DateTime(2026, 10, 9, 12, 30, 0, DateTimeKind.Utc).AddTicks(1234567);
        Assert.Equal(SettingsVersion.Timestamp(time), SettingsVersion.Timestamp(time.AddTicks(-7)));
        Assert.Null(SettingsVersion.Timestamp(null));
    }
}
