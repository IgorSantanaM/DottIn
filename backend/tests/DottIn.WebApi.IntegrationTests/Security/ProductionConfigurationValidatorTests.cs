using DottIn.Presentation.WebApi.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace DottIn.WebApi.IntegrationTests.Security;

public sealed class ProductionConfigurationValidatorTests
{
    [Fact]
    public void Validate_IgnoresNonProductionEnvironment()
    {
        var configuration = new ConfigurationBuilder().Build();

        ProductionConfigurationValidator.Validate(
            configuration,
            new TestHostEnvironment(Environments.Development));
    }

    [Fact]
    public void Validate_RejectsMissingProductionSecrets()
    {
        var configuration = new ConfigurationBuilder().Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(
                configuration,
                new TestHostEnvironment(Environments.Production)));

        Assert.Contains("ConnectionStrings:DottInDb", exception.Message);
        Assert.Contains("JwtSettings:SecretKey", exception.Message);
        Assert.Contains("Stripe:WebhookSecret", exception.Message);
        Assert.Contains("AllowedOrigins", exception.Message);
    }

    [Fact]
    public void Validate_AcceptsCompleteProductionConfiguration()
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DottInDb"] = "Host=db;Database=dottin;Username=dottin;Password=secret",
            ["ConnectionStrings:RabbitMQ"] = "amqps://user:secret@rabbitmq/vhost",
            ["AzureBlob:ConnectionString"] = "UseDevelopmentStorage=false;AccountName=dottin;AccountKey=secret",
            ["AzureBlob:ContainerName"] = "employee-files",
            ["JwtSettings:SecretKey"] = "a-production-secret-with-more-than-32-characters",
            ["AllowedOrigins:0"] = "https://app.dottin.com.br",
            ["Stripe:SecretKey"] = "sk_live_example",
            ["Stripe:PublishableKey"] = "pk_live_example",
            ["Stripe:WebhookSecret"] = "whsec_example",
            ["Stripe:SuccessUrl"] = "https://app.dottin.com.br/billing/success",
            ["Stripe:CancelUrl"] = "https://app.dottin.com.br/billing/cancel",
            ["Stripe:PortalReturnUrl"] = "https://app.dottin.com.br/billing"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        ProductionConfigurationValidator.Validate(
            configuration,
            new TestHostEnvironment(Environments.Production));
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "DottIn.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
