using System.Diagnostics;
using System.Globalization;

namespace DottIn.Presentation.WebApi.Middlewares;

public sealed class RequestPerformanceMiddleware(
    RequestDelegate next,
    ILogger<RequestPerformanceMiddleware> logger)
{
    private const double SlowRequestThresholdMs = 500;

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        context.Response.OnStarting(() =>
        {
            var elapsed = stopwatch.Elapsed.TotalMilliseconds;
            context.Response.Headers["Server-Timing"] =
                $"app;dur={elapsed.ToString("F1", CultureInfo.InvariantCulture)}";
            context.Response.Headers["X-Response-Time-Ms"] =
                elapsed.ToString("F1", CultureInfo.InvariantCulture);
            return Task.CompletedTask;
        });

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            if (stopwatch.Elapsed.TotalMilliseconds >= SlowRequestThresholdMs)
            {
                logger.LogWarning(
                    "Slow request {Method} {Path} returned {StatusCode} in {ElapsedMs:F1} ms. TraceId: {TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.Elapsed.TotalMilliseconds,
                    context.TraceIdentifier);
            }
        }
    }
}