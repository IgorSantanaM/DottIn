using System.Text.Json;
using DottIn.Domain.Core.Exceptions;
using DottIn.Presentation.WebApi.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace DottIn.WebApi.IntegrationTests.Middlewares;

public sealed class ErrorHandlingMiddlewareTests
{
    [Fact]
    public async Task DomainFailure_ReturnsTraceableProblemWithoutLosingMessage()
    {
        var context = Context("trace-domain-123");
        var middleware = new ErrorHandlingMiddleware(
            _ => throw new DomainException("Localização fora do raio permitido."),
            NullLogger<ErrorHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, context.Response.StatusCode);
        Assert.Equal("trace-domain-123", context.Response.Headers["X-Trace-Id"]);
        using var body = await Body(context);
        Assert.Equal("Localização fora do raio permitido.", body.RootElement.GetProperty("title").GetString());
        Assert.Equal("trace-domain-123", body.RootElement.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task UnexpectedFailure_DoesNotExposeInternalExceptionDetails()
    {
        var context = Context("trace-internal-456");
        var middleware = new ErrorHandlingMiddleware(
            _ => throw new InvalidOperationException("database-password-should-not-leak"),
            NullLogger<ErrorHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        using var body = await Body(context);
        Assert.DoesNotContain("database-password", body.RootElement.GetRawText(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("trace-internal-456", body.RootElement.GetProperty("traceId").GetString());
    }

    private static DefaultHttpContext Context(string traceId)
    {
        var context = new DefaultHttpContext { TraceIdentifier = traceId };
        context.Request.Path = "/api/timekeeping/clock-in";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<JsonDocument> Body(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return await JsonDocument.ParseAsync(context.Response.Body);
    }
}
