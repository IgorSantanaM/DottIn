using System.Diagnostics;
using DottIn.Domain.Auth;
using DottIn.Infra.Data.Contexts;
using DottIn.Infra.Services.Auth;
using Microsoft.EntityFrameworkCore;

namespace DottIn.Presentation.WebApi.Performance;

public static class AuthenticationWarmup
{
    // A fixed development-only input used solely to compile and initialize BCrypt.
    // No account can authenticate with this value and no database data is read by it.
    private const string WarmupHash = "$2a$11$YoKSXY1868hVYYatvSmK.OdBnhftFPinSseMKhcQP8xnZwWHs9EOW";

    public static async Task TryWarmAsync(
        IServiceProvider services,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _ = BCrypt.Net.BCrypt.EnhancedVerify("dottin-auth-warmup", WarmupHash);

            await using var scope = services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<DottInContext>();

            // Keep these shapes aligned with the two lookups on the password-login path.
            _ = await db.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(employee => employee.CPF.Value == "00000000000", cancellationToken);
            _ = await db.Branches
                .AsNoTracking()
                .FirstOrDefaultAsync(branch => branch.Id == Guid.Empty, cancellationToken);

            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
            var jwt = configuration.GetSection("JwtSettings");
            _ = tokenService.GenerateToken(
                Guid.Empty,
                Guid.Empty,
                Guid.Empty,
                "Employee",
                jwt["SecretKey"]!,
                jwt["Issuer"]!,
                jwt["Audience"]!,
                int.Parse(jwt["ExpirationMinutes"]!), Guid.Empty);

            // Compile EF's refresh-token INSERT without leaving a token behind. The
            // transaction must be created inside Npgsql's retry strategy callback.
            var executionStrategy = db.Database.CreateExecutionStrategy();
            await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
                db.RefreshTokens.Add(new RefreshToken(Guid.Empty, Guid.Empty, Guid.Empty, expirationDays: 1));
                await db.SaveChangesAsync(cancellationToken);
                await transaction.RollbackAsync(cancellationToken);
                db.ChangeTracker.Clear();
            });

            logger.LogInformation(
                "Authentication warmup completed in {ElapsedMs:F1} ms.",
                stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(
                exception,
                "Authentication warmup was skipped after {ElapsedMs:F1} ms.",
                stopwatch.Elapsed.TotalMilliseconds);
        }
    }
}
