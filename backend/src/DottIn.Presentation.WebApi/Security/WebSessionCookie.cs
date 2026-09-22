namespace DottIn.Presentation.WebApi.Security;

public static class WebSessionCookie
{
    public const string Name = "DottIn.Refresh";
    private const string LocalName = "DottIn.Refresh.Local";

    public static string NameFor(HttpContext context)
        => IsSecure(context) ? Name : LocalName;

    public static void Set(HttpContext context, string refreshToken)
        => context.Response.Cookies.Append(NameFor(context), refreshToken, CreateOptions(context));

    public static void Delete(HttpContext context)
        => context.Response.Cookies.Delete(NameFor(context), CreateOptions(context));

    private static bool IsSecure(HttpContext context)
        => context.Request.IsHttps ||
            !context.Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase);

    private static CookieOptions CreateOptions(HttpContext context)
    {
        // HTTPS APIs may be hosted on a different site from the admin client.
        // Local HTTP development uses Lax so the cookie remains usable without Secure.
        var secure = IsSecure(context);

        return new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = secure ? SameSiteMode.None : SameSiteMode.Lax,
            Path = "/",
            MaxAge = TimeSpan.FromDays(30),
            IsEssential = true
        };
    }
}
