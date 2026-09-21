using DottIn.Application.Features.TimeKeepings;
using DottIn.Application.Shared.DTOS;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.ValueObjects;

namespace DottIn.WebApi.IntegrationTests.TimeKeepings;

public sealed class GeolocationEvidencePolicyTests
{
    private static readonly DateTime NowUtc =
        new(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ValidFreshEvidence_IsAccepted()
    {
        var branch = CreateBranch();
        var evidence = new GeolocationDto(-20.45, -54.62, 15, NowUtc.AddSeconds(-5));

        var location = GeolocationEvidencePolicy.ValidateAndCreate(
            branch, evidence, skipValidation: false, NowUtc);

        Assert.NotNull(location);
        Assert.Equal(evidence.Latitude, location.Latitude);
        Assert.Equal(evidence.Longitude, location.Longitude);
    }

    [Fact]
    public void LowAccuracyEvidence_IsRejected()
    {
        var evidence = new GeolocationDto(-20.45, -54.62, 150, NowUtc);

        var error = Assert.Throws<DomainException>(() =>
            GeolocationEvidencePolicy.ValidateAndCreate(
                CreateBranch(), evidence, skipValidation: false, NowUtc));

        Assert.Contains("precisão", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StaleEvidence_IsRejected()
    {
        var evidence = new GeolocationDto(-20.45, -54.62, 10, NowUtc.AddMinutes(-6));

        var error = Assert.Throws<DomainException>(() =>
            GeolocationEvidencePolicy.ValidateAndCreate(
                CreateBranch(), evidence, skipValidation: false, NowUtc));

        Assert.Contains("desatualizada", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EvidenceOutsideBranchRadius_IsRejected()
    {
        var evidence = new GeolocationDto(-20.40, -54.62, 10, NowUtc);

        Assert.Throws<DomainException>(() =>
            GeolocationEvidencePolicy.ValidateAndCreate(
                CreateBranch(), evidence, skipValidation: false, NowUtc));
    }

    [Fact]
    public void AuthorizedKioskSkip_DoesNotCreateFalseLocationEvidence()
    {
        var evidence = new GeolocationDto(0, 0);

        var location = GeolocationEvidencePolicy.ValidateAndCreate(
            CreateBranch(), evidence, skipValidation: true, NowUtc);

        Assert.Null(location);
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
        email: "teste@dottin.local",
        allowedRadiusMeters: 100);
}
