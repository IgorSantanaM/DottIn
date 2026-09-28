namespace DottIn.WebApi.IntegrationTests.Billing;

public sealed class StripeCheckoutIdempotencyContractTests
{
    [Fact]
    public void CheckoutAttemptGetsFreshKeySharedByItsInternalRetries()
    {
        var source = ReadSource("src/DottIn.Infra.Services/Stripe/StripeService.cs");
        var method = source.IndexOf("public async Task<string> CreateCheckoutSessionAsync(", StringComparison.Ordinal);
        var key = source.IndexOf("var idempotencyKey = $\"checkout_{Guid.NewGuid():N}\";", method, StringComparison.Ordinal);
        var retry = source.IndexOf("return await _retryPipeline.ExecuteAsync(", method, StringComparison.Ordinal);

        Assert.True(method >= 0 && key > method && retry > key);
        Assert.DoesNotContain("yyyy-MM-dd-HH", source, StringComparison.Ordinal);
    }

    private static string ReadSource(string relativePath)
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
        {
            var candidate = Path.Combine(current.FullName, relativePath);
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException($"Could not find source file: {relativePath}");
    }
}
