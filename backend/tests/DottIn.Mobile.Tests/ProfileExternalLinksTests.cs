using DottIn.Mobile.Services;

namespace DottIn.Mobile.Tests;

public sealed class ProfileExternalLinksTests
{
    [Fact]
    public void MissingLinksRemainUnavailable()
    {
        var links = new ProfileExternalLinks(null, "  ", null);

        Assert.Null(links.TermsUrl);
        Assert.Null(links.PrivacyUrl);
        Assert.Null(links.SupportUrl);
    }

    [Fact]
    public void AcceptsHttpsLegalPagesAndSupportEmail()
    {
        var links = new ProfileExternalLinks(
            "https://dottin.example/terms",
            "https://dottin.example/privacy",
            "mailto:suporte@dottin.example");

        Assert.Equal("https://dottin.example/terms", links.TermsUrl?.AbsoluteUri);
        Assert.Equal("https://dottin.example/privacy", links.PrivacyUrl?.AbsoluteUri);
        Assert.Equal("mailto:suporte@dottin.example", links.SupportUrl?.AbsoluteUri);
    }

    [Theory]
    [InlineData("http://dottin.example/terms", null, null)]
    [InlineData(null, "https://user:pass@dottin.example/privacy", null)]
    [InlineData(null, null, "javascript:alert(1)")]
    [InlineData(null, null, "mailto:invalid")]
    public void RejectsUnsafeOrInvalidDestinations(string? terms, string? privacy, string? support)
    {
        Assert.Throws<InvalidOperationException>(() => new ProfileExternalLinks(terms, privacy, support));
    }
}
