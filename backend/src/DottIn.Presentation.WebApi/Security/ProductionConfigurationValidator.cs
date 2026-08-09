namespace DottIn.Presentation.WebApi.Security;

public static class ProductionConfigurationValidator
{
    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsProduction())
            return;

        var errors = new List<string>();

        Require(configuration, "ConnectionStrings:DottInDb", errors);
        Require(configuration, "AzureBlob:ConnectionString", errors);
        Require(configuration, "AzureBlob:ContainerName", errors);
        Require(configuration, "Stripe:SecretKey", errors);
        Require(configuration, "Stripe:PublishableKey", errors);
        Require(configuration, "Stripe:WebhookSecret", errors);

        var jwtSecret = configuration["JwtSettings:SecretKey"];
        if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32 ||
            jwtSecret.Contains("NeedsToBeReplaced", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("JwtSettings:SecretKey deve ser um segredo de produção com pelo menos 32 caracteres.");
        }

        var origins = configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
        if (origins.Length == 0 || origins.Any(origin =>
                !Uri.TryCreate(origin, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps))
        {
            errors.Add("AllowedOrigins deve conter apenas origens HTTPS explícitas.");
        }

        ValidateHttpsUrl(configuration, "Stripe:SuccessUrl", errors);
        ValidateHttpsUrl(configuration, "Stripe:CancelUrl", errors);
        ValidateHttpsUrl(configuration, "Stripe:PortalReturnUrl", errors);

        if (!configuration.GetValue<bool>("MassTransit:Disabled"))
            Require(configuration, "ConnectionStrings:RabbitMQ", errors);

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Configuração de produção inválida:" + Environment.NewLine +
                string.Join(Environment.NewLine, errors.Select(error => $"- {error}")));
        }
    }

    private static void Require(IConfiguration configuration, string key, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(configuration[key]))
            errors.Add($"{key} é obrigatório.");
    }

    private static void ValidateHttpsUrl(IConfiguration configuration, string key, ICollection<string> errors)
    {
        var value = configuration[key];
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            errors.Add($"{key} deve ser uma URL HTTPS válida.");
    }
}
