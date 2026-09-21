using DottIn.Application.Shared.DTOS;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Exceptions;

namespace DottIn.Application.Features.TimeKeepings;

public static class GeolocationEvidencePolicy
{
    public const double MaximumAcceptedAccuracyMeters = 100;
    private static readonly TimeSpan MaximumEvidenceAge = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaximumFutureClockSkew = TimeSpan.FromMinutes(1);

    public static Geolocation? ValidateAndCreate(
        Branch branch,
        GeolocationDto evidence,
        bool skipValidation,
        DateTime nowUtc)
    {
        BranchTime.NormalizeUtc(nowUtc);

        if (skipValidation)
            return null;

        if (evidence.AccuracyMeters is null or <= 0)
            throw new DomainException("A precisão da localização não foi informada.");

        if (evidence.AccuracyMeters > MaximumAcceptedAccuracyMeters)
            throw new DomainException(
                $"A precisão do GPS está baixa ({evidence.AccuracyMeters:0} m). Tente novamente em um local com melhor sinal.");

        if (!evidence.CapturedAtUtc.HasValue)
            throw new DomainException("O horário da captura da localização não foi informado.");

        var capturedAtUtc = BranchTime.NormalizeUtc(evidence.CapturedAtUtc.Value);
        if (capturedAtUtc < nowUtc - MaximumEvidenceAge ||
            capturedAtUtc > nowUtc + MaximumFutureClockSkew)
            throw new DomainException("A localização está desatualizada. Obtenha uma nova posição e tente novamente.");

        if (!branch.IsWithinRange(evidence.Latitude, evidence.Longitude))
            throw new DomainException("Funcionário está fora do raio permitido para bater o ponto.");

        return new Geolocation(evidence.Latitude, evidence.Longitude);
    }
}
