using DottIn.Presentation.WebApi.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Logging;

namespace DottIn.WebApi.IntegrationTests.Middlewares;

public sealed class RequestPerformanceMiddlewareTests
{
    [Fact]
    public async Task SlowRequest_LogsRouteTemplateWithoutCpfFromPath()
    {
        const string cpf = "12345678909";
        const string template = "/api/branches/{branchId}/employees/cpf/{cpf}";
        var context = new DefaultHttpContext();
        context.Request.Path = $"/api/branches/11111111-2222-3333-4444-555555555555/employees/cpf/{cpf}";
        context.SetEndpoint(new RouteEndpointBuilder(
            _ => Task.CompletedTask,
            RoutePatternFactory.Parse(template),
            order: 0).Build());
        var logger = new RecordingLogger();
        var middleware = new RequestPerformanceMiddleware(
            async _ => await Task.Delay(550), logger);

        await middleware.InvokeAsync(context);

        var warning = Assert.Single(logger.Messages);
        Assert.Contains(template, warning);
        Assert.DoesNotContain(cpf, warning);
        Assert.DoesNotContain("11111111-2222-3333-4444-555555555555", warning);
    }

    private sealed class RecordingLogger : ILogger<RequestPerformanceMiddleware>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
                Messages.Add(formatter(state, exception));
        }
    }
}
