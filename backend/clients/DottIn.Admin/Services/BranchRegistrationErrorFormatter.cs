using System.Net;
using System.Text.Json;

namespace DottIn.Admin.Services;

public static class BranchRegistrationErrorFormatter
{
    public static string Format(HttpStatusCode statusCode, string? responseBody)
    {
        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            try
            {
                var problem = JsonSerializer.Deserialize<BranchRegistrationProblem>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var validationMessage = problem?.Errors?.FirstOrDefault()?.Error;
                if (!string.IsNullOrWhiteSpace(validationMessage))
                    return validationMessage;

                if (!string.IsNullOrWhiteSpace(problem?.Title))
                    return problem.Title;
            }
            catch (JsonException)
            {
                // The HTTP status below identifies non-API responses, such as a proxy or authorization page.
            }
        }

        return $"Não foi possível registrar a filial (HTTP {(int)statusCode} {statusCode}).";
    }

    private sealed record BranchRegistrationProblem(
        string? Title,
        IReadOnlyList<BranchRegistrationValidationError>? Errors);

    private sealed record BranchRegistrationValidationError(string? Field, string? Error);
}
