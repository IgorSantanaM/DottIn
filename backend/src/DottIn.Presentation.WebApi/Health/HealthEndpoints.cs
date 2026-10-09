using DottIn.Infra.Data.Contexts;

namespace DottIn.Presentation.WebApi.Health;

public static class HealthEndpoints
{
    public static void Map(WebApplication app)
    {
        // /api is a discoverable API root, not the Admin SPA or a second prefix.
        foreach (var path in new[] { "/", "/api" })
            app.MapGet(path, GetServiceInfo).AllowAnonymous().ExcludeFromDescription();

        foreach (var path in new[] { "/health/live", "/api/health/live" })
            app.MapGet(path, GetLiveness).AllowAnonymous().ExcludeFromDescription();

        foreach (var path in new[] { "/health/ready", "/api/health/ready" })
            app.MapGet(path, GetReadinessAsync).AllowAnonymous().ExcludeFromDescription();
    }

    private static IResult GetServiceInfo() => Results.Ok(new { service = "DottIn API", status = "healthy" });
    private static IResult GetLiveness() => Results.Ok(new { status = "healthy" });

    private static async Task<IResult> GetReadinessAsync(DottInContext dbContext, CancellationToken cancellationToken)
        => await DatabaseReadinessProbe.IsReadyAsync(
            token => dbContext.Database.CanConnectAsync(token), cancellationToken)
            ? Results.Ok(new { status = "ready" })
            : Results.Json(new { status = "unavailable", dependency = "database" },
                statusCode: StatusCodes.Status503ServiceUnavailable);
}
