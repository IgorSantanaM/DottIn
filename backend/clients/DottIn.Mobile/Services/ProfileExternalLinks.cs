namespace DottIn.Mobile.Services;

public sealed class ProfileExternalLinks
{
    public Uri? TermsUrl { get; }
    public Uri? PrivacyUrl { get; }
    public Uri? SupportUrl { get; }

    public ProfileExternalLinks(string? termsUrl, string? privacyUrl, string? supportUrl)
    {
        TermsUrl = Parse(termsUrl, "Termos de Uso", allowMailto: false);
        PrivacyUrl = Parse(privacyUrl, "Política de Privacidade", allowMailto: false);
        SupportUrl = Parse(supportUrl, "Suporte", allowMailto: true);
    }

    private static Uri? Parse(string? value, string label, bool allowMailto)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
            throw InvalidLink(label, allowMailto);

        var isHttps = uri.Scheme == Uri.UriSchemeHttps &&
                      !string.IsNullOrWhiteSpace(uri.Host) &&
                      string.IsNullOrEmpty(uri.UserInfo);
        var recipient = uri.Scheme == Uri.UriSchemeMailto
            ? uri.AbsoluteUri["mailto:".Length..].Split('?', 2)[0]
            : string.Empty;
        var isMailto = allowMailto && uri.Scheme == Uri.UriSchemeMailto &&
                       System.Net.Mail.MailAddress.TryCreate(recipient, out _);
        if (!isHttps && !isMailto)
            throw InvalidLink(label, allowMailto);

        return uri;
    }

    private static InvalidOperationException InvalidLink(string label, bool allowMailto)
        => new($"Configure um endereço HTTPS válido para {label}" +
            (allowMailto ? " ou um endereço mailto de suporte." : "."));
}