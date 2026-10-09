using System.Net;
using DottIn.Presentation.WebApi.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using IPNetwork = System.Net.IPNetwork;

namespace DottIn.WebApi.IntegrationTests.Security;

public sealed class ReverseProxyConfigurationTests
{
    [Theory]
    [InlineData("172.19.0.5", 16, "172.19.0.0/16")]
    [InlineData("172.28.85.2", 24, "172.28.85.0/24")]
    [InlineData("10.85.42.9", 24, "10.85.42.0/24")]
    [InlineData("192.168.42.5", 20, "192.168.32.0/20")]
    public void GetPrivateNetwork_MasksTheAssignedAddress(string address, int prefix, string expected)
    {
        Assert.Equal(IPNetwork.Parse(expected), ReverseProxyConfiguration.GetPrivateNetwork(IPAddress.Parse(address), prefix));
    }

    [Theory]
    [InlineData("127.0.0.1", 8)]
    [InlineData("8.8.8.8", 24)]
    [InlineData("169.254.42.5", 16)]
    [InlineData("10.42.5.1", 7)]
    [InlineData("172.19.5.1", 11)]
    [InlineData("192.168.42.5", 15)]
    [InlineData("::1", 128)]
    [InlineData("10.42.5.1", 33)]
    public void GetPrivateNetwork_RejectsNonPrivateOrOverlyBroadRanges(string address, int prefix)
    {
        Assert.Null(ReverseProxyConfiguration.GetPrivateNetwork(IPAddress.Parse(address), prefix));
    }

    [Fact]
    public void Configure_TrustsAssignedNetworkButNotOtherPrivateNetworks()
    {
        var options = new ForwardedHeadersOptions();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ReverseProxy:TrustContainerNetwork"] = "true"
        }).Build();

        ReverseProxyConfiguration.Configure(options, configuration, [IPNetwork.Parse("172.19.0.0/16")]);

        Assert.Contains(options.KnownIPNetworks, network => network.Contains(IPAddress.Parse("172.19.0.5")));
        Assert.DoesNotContain(options.KnownIPNetworks, network => network.Contains(IPAddress.Parse("172.20.0.5")));
        Assert.Equal(2, options.ForwardLimit);
        Assert.Equal(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto, options.ForwardedHeaders);
    }

    [Fact]
    public void Configure_WithoutOptInRetainsExplicitTrustOnly()
    {
        var options = new ForwardedHeadersOptions();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ReverseProxy:KnownNetworks:0"] = "10.42.0.0/24"
        }).Build();

        ReverseProxyConfiguration.Configure(options, configuration, [IPNetwork.Parse("172.19.0.0/16")]);

        Assert.Contains(options.KnownIPNetworks, network => network.Contains(IPAddress.Parse("10.42.0.5")));
        Assert.DoesNotContain(options.KnownIPNetworks, network => network.Contains(IPAddress.Parse("172.19.0.5")));
    }

    [Fact]
    public void Configure_RejectsEmptyDiscoveryInsteadOfTrustingEveryProxy()
    {
        var options = new ForwardedHeadersOptions();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ReverseProxy:TrustContainerNetwork"] = "true"
        }).Build();

        Assert.Throws<InvalidOperationException>(() => ReverseProxyConfiguration.Configure(options, configuration, []));
    }
}
