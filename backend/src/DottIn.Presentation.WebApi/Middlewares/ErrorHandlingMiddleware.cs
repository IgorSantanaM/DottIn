using DottIn.Application.Exceptions;
using DottIn.Domain.Core.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace DottIn.Presentation.WebApi.Middlewares
{
    public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            context.Response.Headers["X-Trace-Id"] = context.TraceIdentifier;
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                var expectedFailure = ex is DomainException or ValidationException or NotFoundException or ArgumentException or BadHttpRequestException or BreakOutsideAllowedTimeException;
                if (expectedFailure && context.Request.Path.StartsWithSegments("/api/timekeeping"))
                    logger.LogWarning(
                        "Timekeeping request rejected. Path: {Path}; TraceId: {TraceId}; Reason: {Reason}",
                        context.Request.Path, context.TraceIdentifier, ex.Message);
                else if (!expectedFailure)
                    logger.LogError(ex, "Unhandled request failure. TraceId: {TraceId}", context.TraceIdentifier);

                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, title, errors) = exception switch
            {
                ArgumentException argumentException => (
                    HttpStatusCode.BadRequest,
                    argumentException.Message,
                    null
                ),
                ValidationException validationException => (
                    HttpStatusCode.BadRequest,
                    "Validation failed",
                    validationException.Errors.Select(e => new { field = e.PropertyName, error = e.ErrorMessage })
                ),
                DomainException domainException => (
                    HttpStatusCode.UnprocessableEntity,
                    domainException.Message,
                    null
                ),
                NotFoundException notFoundException => (
                    HttpStatusCode.NotFound,
                    notFoundException.Message,
                    null
                ),
                BadHttpRequestException badHttpRequestException => (
                    HttpStatusCode.BadRequest,
                    badHttpRequestException.Message,
                    null
                ),
                BreakOutsideAllowedTimeException breakOutsideAllowedTimeException => (
                    HttpStatusCode.UnprocessableEntity,
                    breakOutsideAllowedTimeException.Message,
                    null
                ),
                DbUpdateConcurrencyException => (
                    HttpStatusCode.Conflict,
                    "O registro foi alterado por outra operação. Atualize os dados e tente novamente.",
                    null
                ),
                DbUpdateException => (
                    HttpStatusCode.Conflict,
                    "A operação conflita com um registro já existente.",
                    null
                ),
                _ => (
                    HttpStatusCode.InternalServerError,
                    "An unexpected error occurred, try again later.",
                    null
                )
            };

            var problem = new
            {
                Status = (int)statusCode,
                Title = title,
                Errors = errors,
                TraceId = context.TraceIdentifier
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = problem.Status;

            var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            await context.Response.WriteAsync(json);
        }
    }
}
