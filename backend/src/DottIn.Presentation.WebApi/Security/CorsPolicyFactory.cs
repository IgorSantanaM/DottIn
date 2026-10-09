using Microsoft.AspNetCore.Cors.Infrastructure;

namespace DottIn.Presentation.WebApi.Security;

public static class CorsPolicyFactory
{
    public static CorsPolicy Create(IEnumerable<string> origins)
    {
        var normalized = origins.Select(origin =>
        {
            if (!TryNormalizeOrigin(origin, out var value))
                throw new InvalidOperationException("AllowedOrigins must contain explicit HTTP/HTTPS origins, without paths or wildcards.");
            return value;
        }).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        return new CorsPolicyBuilder()
            .WithOrigins(normalized)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .Build();
    }

    public static bool TryNormalizeOrigin(string? origin, out string normalized)
    {
        normalized = string.Empty;
        if (!Uri.TryCreate(origin?.Trim(), UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https") || uri.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) ||
            !string.IsNullOrEmpty(uri.UserInfo) || uri.Host.Contains('*'))
            return false;

        normalized = uri.GetLeftPart(UriPartial.Authority);
        return true;
    }
}
